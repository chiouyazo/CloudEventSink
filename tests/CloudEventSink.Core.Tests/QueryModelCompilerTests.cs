using System.Text.Json.Nodes;
using CloudEventSink.Core.Query;
using CloudEventSink.Core.Query.Sql;

namespace CloudEventSink.Core.Tests;

public sealed class QueryModelCompilerTests
{
    private readonly QueryModelCompiler compiler = new QueryModelCompiler();

    private static ProjectionCatalog Catalog()
    {
        Dictionary<string, string> main = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["event_id"] = "uuid",
            ["device_id"] = "text",
            ["total"] = "numeric",
            ["active"] = "boolean",
        };
        Dictionary<string, string> items = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["event_id"] = "uuid",
            ["item_id"] = "text",
            ["qty"] = "numeric",
        };

        return new ProjectionCatalog
        {
            Views = new Dictionary<string, IReadOnlyDictionary<string, string>>(
                StringComparer.Ordinal
            )
            {
                ["v_main"] = main,
                ["v_main_items"] = items,
            },
        };
    }

    [Fact]
    public void EmptyColumns_SelectsAllFromBaseView()
    {
        GeneratedSql sql = this.compiler.Compile(new QueryModel { BaseView = "v_main" }, Catalog());

        Assert.Contains(
            "SELECT \"v_main\".* FROM \"v_main\"",
            sql.CommandText,
            StringComparison.Ordinal
        );
        Assert.Contains("device_id", sql.ResultColumns);
    }

    [Fact]
    public void AggregateWithGroupBy_ProducesGroupedSql()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Columns =
            [
                new SelectColumn { View = "v_main", Column = "device_id" },
                new SelectColumn
                {
                    View = "v_main",
                    Column = "total",
                    Aggregate = AggregateFunction.Sum,
                    Alias = "total_sum",
                },
            ],
            GroupBy = [new ColumnRef { View = "v_main", Column = "device_id" }],
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains(
            "sum(\"v_main\".\"total\") AS \"total_sum\"",
            sql.CommandText,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "GROUP BY \"v_main\".\"device_id\"",
            sql.CommandText,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Join_ProducesJoinClause()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Joins =
            [
                new QueryJoin
                {
                    Type = JoinType.Left,
                    TargetView = "v_main_items",
                    LeftView = "v_main",
                    LeftColumn = "event_id",
                    RightView = "v_main_items",
                    RightColumn = "event_id",
                },
            ],
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains(
            "LEFT JOIN \"v_main_items\" ON \"v_main\".\"event_id\" = \"v_main_items\".\"event_id\"",
            sql.CommandText,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Filter_KeepsValueInParameterOnly()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Filters = new FilterGroup
            {
                Conditions =
                [
                    new FilterCondition
                    {
                        View = "v_main",
                        Column = "device_id",
                        Operator = ConditionOperator.Eq,
                        Value = JsonValue.Create("node-a"),
                    },
                ],
            },
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains(
            "\"v_main\".\"device_id\" = @p0",
            sql.CommandText,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("node-a", sql.CommandText, StringComparison.Ordinal);
        Assert.Equal("node-a", Assert.Single(sql.Parameters).Value);
    }

    [Fact]
    public void MaliciousValue_NeverReachesCommandText()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Filters = new FilterGroup
            {
                Conditions =
                [
                    new FilterCondition
                    {
                        View = "v_main",
                        Column = "device_id",
                        Operator = ConditionOperator.Eq,
                        Value = JsonValue.Create("x'; DROP TABLE events; --"),
                    },
                ],
            },
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.DoesNotContain("DROP TABLE", sql.CommandText, StringComparison.Ordinal);
        Assert.Contains(
            "x'; DROP TABLE events; --",
            sql.Parameters.Select(parameter => parameter.Value?.ToString())
        );
    }

    [Fact]
    public void InOperator_UsesArrayParameter()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Filters = new FilterGroup
            {
                Conditions =
                [
                    new FilterCondition
                    {
                        View = "v_main",
                        Column = "total",
                        Operator = ConditionOperator.In,
                        Values = [JsonValue.Create(1), JsonValue.Create(2)],
                    },
                ],
            },
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains("= ANY(@p0)", sql.CommandText, StringComparison.Ordinal);
        Assert.Equal(SqlParameterType.NumericArray, Assert.Single(sql.Parameters).Type);
    }

    [Fact]
    public void UuidFilter_CastsParameterToUuid()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Filters = new FilterGroup
            {
                Conditions =
                [
                    new FilterCondition
                    {
                        View = "v_main",
                        Column = "event_id",
                        Operator = ConditionOperator.Eq,
                        Value = JsonValue.Create("11111111-1111-1111-1111-111111111111"),
                    },
                ],
            },
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains(
            "\"v_main\".\"event_id\" = @p0::uuid",
            sql.CommandText,
            StringComparison.Ordinal
        );
        Assert.Equal(SqlParameterType.Text, Assert.Single(sql.Parameters).Type);
    }

    [Fact]
    public void UuidInOperator_CastsArrayToUuidArray()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Filters = new FilterGroup
            {
                Conditions =
                [
                    new FilterCondition
                    {
                        View = "v_main",
                        Column = "event_id",
                        Operator = ConditionOperator.In,
                        Values =
                        [
                            JsonValue.Create("11111111-1111-1111-1111-111111111111"),
                            JsonValue.Create("22222222-2222-2222-2222-222222222222"),
                        ],
                    },
                ],
            },
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains("= ANY(@p0::uuid[])", sql.CommandText, StringComparison.Ordinal);
        Assert.Equal(SqlParameterType.TextArray, Assert.Single(sql.Parameters).Type);
    }

    [Fact]
    public void MultipleJoins_ProduceMultipleJoinClauses()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Joins =
            [
                new QueryJoin
                {
                    Type = JoinType.Left,
                    TargetView = "v_main_items",
                    LeftView = "v_main",
                    LeftColumn = "event_id",
                    RightView = "v_main_items",
                    RightColumn = "event_id",
                },
                new QueryJoin
                {
                    Type = JoinType.Inner,
                    TargetView = "v_main_items",
                    LeftView = "v_main",
                    LeftColumn = "device_id",
                    RightView = "v_main_items",
                    RightColumn = "item_id",
                },
            ],
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains("LEFT JOIN \"v_main_items\"", sql.CommandText, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN \"v_main_items\"", sql.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void ColumnAlias_IsEmitted()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Columns =
            [
                new SelectColumn
                {
                    View = "v_main",
                    Column = "device_id",
                    Alias = "device_id_2",
                },
            ],
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains("AS \"device_id_2\"", sql.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void CompositeJoinKeys_EmitAndedConditions()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Joins =
            [
                new QueryJoin
                {
                    Type = JoinType.Inner,
                    TargetView = "v_main_items",
                    LeftView = "v_main",
                    LeftColumn = "event_id",
                    RightView = "v_main_items",
                    RightColumn = "event_id",
                    Keys =
                    [
                        new JoinKeyPair { LeftColumn = "event_id", RightColumn = "event_id" },
                        new JoinKeyPair { LeftColumn = "device_id", RightColumn = "item_id" },
                    ],
                },
            ],
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains(
            "\"v_main\".\"event_id\" = \"v_main_items\".\"event_id\" AND \"v_main\".\"device_id\" = \"v_main_items\".\"item_id\"",
            sql.CommandText,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void DistinctOn_WrapsBaseInLatestCteBeforeJoins()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            DistinctOn = [new ColumnRef { View = "v_main", Column = "device_id" }],
            LatestBy = new ColumnRef { View = "v_main", Column = "total" },
            Joins =
            [
                new QueryJoin
                {
                    Type = JoinType.Left,
                    TargetView = "v_main_items",
                    LeftView = "v_main",
                    LeftColumn = "event_id",
                    RightView = "v_main_items",
                    RightColumn = "event_id",
                },
            ],
        };

        GeneratedSql sql = this.compiler.Compile(model, Catalog());

        Assert.Contains(
            "WITH \"__latest\" AS (SELECT DISTINCT ON (\"v_main\".\"device_id\") * FROM \"v_main\" ORDER BY \"v_main\".\"device_id\" ASC, \"v_main\".\"total\" DESC)",
            sql.CommandText,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "FROM \"__latest\" AS \"v_main\"",
            sql.CommandText,
            StringComparison.Ordinal
        );
        Assert.Contains("LEFT JOIN \"v_main_items\"", sql.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownView_Throws()
    {
        Assert.Throws<QueryValidationException>(() =>
            this.compiler.Compile(new QueryModel { BaseView = "nope" }, Catalog())
        );
    }

    [Fact]
    public void UnknownColumn_Throws()
    {
        QueryModel model = new QueryModel
        {
            BaseView = "v_main",
            Columns = [new SelectColumn { View = "v_main", Column = "ghost" }],
        };

        Assert.Throws<QueryValidationException>(() => this.compiler.Compile(model, Catalog()));
    }
}
