using System.Text.Json;
using CloudEventSink.Core.Projection;
using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Tests;

public sealed class ProjectionGeneratorTests
{
    private static readonly Guid SourceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly ProjectionGenerator generator = new ProjectionGenerator();
    private readonly SchemaInferenceService inference = new SchemaInferenceService();

    [Fact]
    public void MainView_FlattensScalarsAndNestedObjects()
    {
        ProjectionPlan plan = Generate(
            """{ "deviceId": "x", "address": { "city": "y" }, "total": 5 }"""
        );

        Assert.Equal("v_portal_hook_order_created", plan.MainView.Name);
        Assert.Contains(plan.MainView.Columns, column => column.Name == "event_id");
        Assert.Contains(plan.MainView.Columns, column => column.Name == "received_at");
        Assert.Contains(plan.MainView.Columns, column => column.Name == "time");

        ProjectedColumn device = plan.MainView.Columns.Single(column => column.Name == "device_id");
        Assert.Equal("text", device.SqlType);

        ProjectedColumn city = plan.MainView.Columns.Single(column =>
            column.Name == "address_city"
        );
        Assert.Equal(new[] { "address", "city" }, city.Path);

        ProjectedColumn total = plan.MainView.Columns.Single(column => column.Name == "total");
        Assert.Equal("numeric", total.SqlType);
    }

    [Fact]
    public void ArrayOfObjects_ProducesChildViewWithBackreference()
    {
        ProjectionPlan plan = Generate("""{ "items": [ { "itemId": "a1", "channel": null } ] }""");

        ProjectedView child = Assert.Single(plan.ChildViews);
        Assert.True(child.IsChild);
        Assert.False(child.ScalarArray);
        Assert.Equal(new[] { "items" }, child.ArrayPath);
        Assert.Contains(child.Columns, column => column.Name == "event_id");
        Assert.Contains(child.Columns, column => column.Name == "item_id");
        Assert.Contains(child.Columns, column => column.Name == "channel");
        Assert.DoesNotContain(plan.MainView.Columns, column => column.Name == "items");
        Assert.Contains("jsonb_array_elements(", plan.CreateSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ArrayOfScalars_ProducesScalarChildView()
    {
        ProjectionPlan plan = Generate("""{ "tags": [ "a", "b" ] }""");

        ProjectedView child = Assert.Single(plan.ChildViews);
        Assert.True(child.ScalarArray);
        Assert.Contains(child.Columns, column => column.Name == "value");
        Assert.Contains("jsonb_array_elements_text(", plan.CreateSql, StringComparison.Ordinal);
    }

    [Fact]
    public void NumericAndBoolean_ProduceCasts()
    {
        ProjectionPlan plan = Generate("""{ "n": 3, "b": true, "s": "x" }""");

        Assert.Equal("numeric", plan.MainView.Columns.Single(column => column.Name == "n").SqlType);
        Assert.Equal("boolean", plan.MainView.Columns.Single(column => column.Name == "b").SqlType);
        Assert.Equal("text", plan.MainView.Columns.Single(column => column.Name == "s").SqlType);
        Assert.Contains("::numeric", plan.CreateSql, StringComparison.Ordinal);
        Assert.Contains("::boolean", plan.CreateSql, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyWithSingleQuote_IsEscapedInPathLiteralAndSanitizedIdentifier()
    {
        ProjectionPlan plan = Generate("""{ "e'vil": 1 }""");

        Assert.Contains("'e''vil'", plan.CreateSql, StringComparison.Ordinal);
        Assert.DoesNotContain("\"e'vil\"", plan.CreateSql, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateSql_ScopesToSourceAndEventType()
    {
        ProjectionPlan plan = Generate("""{ "a": 1 }""");

        Assert.Contains("CREATE OR REPLACE VIEW", plan.CreateSql, StringComparison.Ordinal);
        Assert.Contains($"'{SourceId}'::uuid", plan.CreateSql, StringComparison.Ordinal);
        Assert.Contains("'order.created'", plan.CreateSql, StringComparison.Ordinal);
    }

    private ProjectionPlan Generate(string dataJson)
    {
        using JsonDocument document = JsonDocument.Parse(dataJson);
        FieldNode root = this.inference.Derive(document.RootElement);
        return this.generator.BuildDefaultPlan(SourceId, "portal-hook", "order.created", root);
    }
}
