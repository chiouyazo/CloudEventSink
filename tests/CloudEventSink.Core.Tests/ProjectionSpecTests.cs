using System.Text.Json;
using CloudEventSink.Core.Projection;
using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Tests;

public sealed class ProjectionSpecTests
{
    private static readonly Guid SourceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly ProjectionGenerator generator = new ProjectionGenerator();
    private readonly SchemaInferenceService inference = new SchemaInferenceService();

    [Fact]
    public void BuildDefault_MainAndChildTables()
    {
        ProjectionSpec spec = BuildSpec("""{ "deviceId": "x", "items": [ { "itemId": "a" } ] }""");

        TableSpec main = spec.Tables.Single(table => !table.IsChild);
        Assert.Equal(ProjectionSpecFactory.MainKey, main.Key);
        Assert.Contains(main.Columns, column => column.Role == ColumnRole.EventId);
        Assert.Contains(
            main.Columns,
            column => column.Name == "device_id" && column.Role == ColumnRole.Scalar
        );

        TableSpec items = spec.Tables.Single(table => table.Key == "items");
        Assert.True(items.IsChild);
        Assert.Equal(ProjectionSpecFactory.MainKey, items.ParentKey);
        Assert.Contains(items.Columns, column => column.Role == ColumnRole.Ordinal);
        Assert.Contains(items.Columns, column => column.Name == "item_id");
    }

    [Fact]
    public void RenameAndTypeOverride_AreHonored()
    {
        ProjectionSpec spec = BuildSpec("""{ "deviceId": "x", "count": "5" }""");
        TableSpec main = spec.Tables[0];
        List<ColumnSpec> columns = main
            .Columns.Select(column =>
                column.Name == "device_id" ? column with { Name = "machine" } : column
            )
            .Select(column => column.Name == "count" ? column with { SqlType = "numeric" } : column)
            .ToList();
        ProjectionSpec edited = spec with
        {
            Tables = [main with { Name = "renamed_view", Columns = columns }],
        };

        ProjectionPlan plan = this.generator.Generate(SourceId, "order.created", edited);

        Assert.Equal("renamed_view", plan.MainView.Name);
        Assert.Contains("AS \"machine\"", plan.CreateSql, StringComparison.Ordinal);
        Assert.Contains("::numeric", plan.CreateSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcludedColumn_IsNotEmitted()
    {
        ProjectionSpec spec = BuildSpec("""{ "keep": "a", "drop": "b" }""");
        TableSpec main = spec.Tables[0];
        List<ColumnSpec> columns = main
            .Columns.Select(column =>
                column.Name == "drop" ? column with { Included = false } : column
            )
            .ToList();
        ProjectionPlan plan = this.generator.Generate(
            SourceId,
            "e",
            spec with
            {
                Tables = [main with { Columns = columns }],
            }
        );

        Assert.DoesNotContain("AS \"drop\"", plan.CreateSql, StringComparison.Ordinal);
        Assert.Contains("AS \"keep\"", plan.CreateSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ArrayAsJsonColumn_AddsJsonbColumnToParentAndNoChildView()
    {
        ProjectionSpec spec = BuildSpec("""{ "items": [ { "itemId": "a" } ] }""");
        ProjectionSpec edited = spec with
        {
            Tables = spec
                .Tables.Select(table =>
                    table.Key == "items" ? table with { Mode = ArrayMode.JsonColumn } : table
                )
                .ToList(),
        };

        ProjectionPlan plan = this.generator.Generate(SourceId, "e", edited);

        Assert.Empty(plan.ChildViews);
        Assert.Contains(plan.MainView.Columns, column => column.SqlType == "jsonb");
    }

    [Fact]
    public void ArrayExcluded_ProducesNoChildView()
    {
        ProjectionSpec spec = BuildSpec("""{ "items": [ { "itemId": "a" } ] }""");
        ProjectionSpec edited = spec with
        {
            Tables = spec
                .Tables.Select(table =>
                    table.Key == "items" ? table with { Mode = ArrayMode.Exclude } : table
                )
                .ToList(),
        };

        ProjectionPlan plan = this.generator.Generate(SourceId, "e", edited);

        Assert.Empty(plan.ChildViews);
    }

    [Fact]
    public void NestedArray_ProducesGrandchildWithOrdinality()
    {
        ProjectionSpec spec = BuildSpec(
            """{ "products": [ { "id": "p", "licenses": [ { "key": "k" } ] } ] }"""
        );

        Assert.Contains(spec.Tables, table => table.Key == "products");
        TableSpec grandchild = spec.Tables.Single(table => table.Key == "products.licenses");
        Assert.Equal("products", grandchild.ParentKey);
        Assert.Equal(2, grandchild.Columns.Count(column => column.Role == ColumnRole.Ordinal));

        ProjectionPlan plan = this.generator.Generate(SourceId, "e", spec);
        ProjectedView grandchildView = plan.ChildViews.Single(view => view.ArrayPath.Count == 2);
        Assert.Equal(3, CountOccurrences(plan.CreateSql, "WITH ORDINALITY"));
        Assert.Contains("a2.value", plan.CreateSql, StringComparison.Ordinal);
        Assert.True(grandchildView.Columns.Count(column => column.SqlType == "bigint") >= 2);
    }

    [Fact]
    public void ScalarArray_UsesElementsText()
    {
        ProjectionSpec spec = BuildSpec("""{ "tags": [ "a", "b" ] }""");
        ProjectionPlan plan = this.generator.Generate(SourceId, "e", spec);

        ProjectedView child = Assert.Single(plan.ChildViews);
        Assert.True(child.ScalarArray);
        Assert.Contains("jsonb_array_elements_text(", plan.CreateSql, StringComparison.Ordinal);
        Assert.Contains(child.Columns, column => column.Name == "value");
    }

    [Fact]
    public void Merge_AddsNewFieldsKeepsEditsDeletesNothing()
    {
        ProjectionSpec original = BuildSpec("""{ "a": "1" }""");
        TableSpec main = original.Tables[0];
        List<ColumnSpec> edited = main
            .Columns.Select(column =>
                column.Name == "a" ? column with { Name = "renamed_a" } : column
            )
            .ToList();
        ProjectionSpec userSpec = original with { Tables = [main with { Columns = edited }] };

        ProjectionSpec fresh = BuildSpec("""{ "a": "1", "b": "2" }""");
        ProjectionSpec merged = ProjectionSpecMerger.Merge(userSpec, fresh);

        TableSpec mergedMain = merged.Tables[0];
        Assert.Contains(mergedMain.Columns, column => column.Name == "renamed_a");
        Assert.Contains(
            mergedMain.Columns,
            column => column.SourcePath.Count == 1 && column.SourcePath[0] == "b"
        );
    }

    private ProjectionSpec BuildSpec(string dataJson)
    {
        using JsonDocument document = JsonDocument.Parse(dataJson);
        FieldNode root = this.inference.Derive(document.RootElement);
        return ProjectionSpecFactory.BuildDefault("demo-source", "order.created", root);
    }

    private static int CountOccurrences(string text, string token)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
