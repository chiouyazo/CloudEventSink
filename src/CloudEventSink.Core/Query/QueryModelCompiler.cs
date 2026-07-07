using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CloudEventSink.Core.Query.Sql;

namespace CloudEventSink.Core.Query;

public sealed class QueryModelCompiler : IQueryModelCompiler
{
    private static readonly Regex AliasPattern = new Regex(
        "^[A-Za-z0-9_]{1,64}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200)
    );

    public GeneratedSql Compile(QueryModel model, ProjectionCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(catalog);

        if (!catalog.HasView(model.BaseView))
        {
            throw new QueryValidationException($"Unknown view '{model.BaseView}'.");
        }

        List<SqlParameterSpec> parameters = new List<SqlParameterSpec>();
        List<string> resultColumns = new List<string>();
        StringBuilder sql = new StringBuilder();

        bool latestPerKey = model.DistinctOn.Count > 0;
        AppendLatestCte(model, catalog, sql, latestPerKey);
        sql.Append("SELECT ");
        AppendSelect(model, catalog, resultColumns, sql);
        if (latestPerKey)
        {
            sql.Append(CultureInfo.InvariantCulture, $" FROM \"__latest\" AS \"{model.BaseView}\"");
        }
        else
        {
            sql.Append(CultureInfo.InvariantCulture, $" FROM \"{model.BaseView}\"");
        }

        AppendJoins(model, catalog, sql);
        AppendWhere(model, catalog, parameters, sql);
        AppendGroupBy(model, catalog, sql);
        AppendHaving(model, catalog, parameters, sql);
        AppendOrderBy(model, catalog, sql);
        AppendLimit(model, sql);

        return new GeneratedSql
        {
            CommandText = sql.ToString(),
            Parameters = parameters,
            ResultColumns = resultColumns,
        };
    }

    private static void AppendSelect(
        QueryModel model,
        ProjectionCatalog catalog,
        List<string> resultColumns,
        StringBuilder sql
    )
    {
        if (model.Columns.Count == 0)
        {
            List<string> stars = new List<string> { $"\"{model.BaseView}\".*" };
            foreach (string column in catalog.Views[model.BaseView].Keys)
            {
                resultColumns.Add(column);
            }

            IEnumerable<string> targets = model
                .Joins.Select(join => join.TargetView)
                .Where(view =>
                    catalog.HasView(view)
                    && !string.Equals(view, model.BaseView, StringComparison.Ordinal)
                )
                .Distinct(StringComparer.Ordinal);
            foreach (string target in targets)
            {
                stars.Add($"\"{target}\".*");
                foreach (string column in catalog.Views[target].Keys)
                {
                    resultColumns.Add(column);
                }
            }

            sql.Append(string.Join(", ", stars));
            return;
        }

        List<string> selects = new List<string>();
        foreach (SelectColumn column in model.Columns)
        {
            ValidateColumn(catalog, column.View, column.Column);
            string columnExpression = $"\"{column.View}\".\"{column.Column}\"";
            string expression = column.Aggregate.HasValue
                ? $"{AggregateName(column.Aggregate.Value)}({columnExpression})"
                : columnExpression;
            string alias = ResolveAlias(column);
            selects.Add($"{expression} AS \"{alias}\"");
            resultColumns.Add(alias);
        }

        sql.Append(string.Join(", ", selects));
    }

    private static void AppendJoins(QueryModel model, ProjectionCatalog catalog, StringBuilder sql)
    {
        foreach (QueryJoin join in model.Joins)
        {
            if (!catalog.HasView(join.TargetView))
            {
                throw new QueryValidationException($"Unknown view '{join.TargetView}'.");
            }

            IReadOnlyList<JoinKeyPair> keys =
                join.Keys.Count > 0
                    ? join.Keys
                    :
                    [
                        new JoinKeyPair
                        {
                            LeftColumn = join.LeftColumn,
                            RightColumn = join.RightColumn,
                        },
                    ];

            List<string> conditions = new List<string>();
            foreach (JoinKeyPair key in keys)
            {
                ValidateColumn(catalog, join.LeftView, key.LeftColumn);
                ValidateColumn(catalog, join.RightView, key.RightColumn);
                conditions.Add(
                    $"\"{join.LeftView}\".\"{key.LeftColumn}\" = \"{join.RightView}\".\"{key.RightColumn}\""
                );
            }

            string joinType = join.Type == JoinType.Inner ? "INNER JOIN" : "LEFT JOIN";
            sql.Append(CultureInfo.InvariantCulture, $" {joinType} \"{join.TargetView}\" ON ");
            sql.Append(string.Join(" AND ", conditions));
        }
    }

    private static void AppendWhere(
        QueryModel model,
        ProjectionCatalog catalog,
        List<SqlParameterSpec> parameters,
        StringBuilder sql
    )
    {
        if (model.Filters is null)
        {
            return;
        }

        string where = BuildGroup(model.Filters, catalog, parameters);
        if (!string.IsNullOrEmpty(where))
        {
            sql.Append(" WHERE ").Append(where);
        }
    }

    private static void AppendGroupBy(
        QueryModel model,
        ProjectionCatalog catalog,
        StringBuilder sql
    )
    {
        if (model.GroupBy.Count == 0)
        {
            return;
        }

        List<string> groupBy = new List<string>();
        foreach (ColumnRef reference in model.GroupBy)
        {
            ValidateColumn(catalog, reference.View, reference.Column);
            groupBy.Add($"\"{reference.View}\".\"{reference.Column}\"");
        }

        sql.Append(" GROUP BY ").Append(string.Join(", ", groupBy));
    }

    private static void AppendHaving(
        QueryModel model,
        ProjectionCatalog catalog,
        List<SqlParameterSpec> parameters,
        StringBuilder sql
    )
    {
        if (model.Having is null)
        {
            return;
        }

        string having = BuildGroup(model.Having, catalog, parameters);
        if (!string.IsNullOrEmpty(having))
        {
            sql.Append(" HAVING ").Append(having);
        }
    }

    private static void AppendLatestCte(
        QueryModel model,
        ProjectionCatalog catalog,
        StringBuilder sql,
        bool latestPerKey
    )
    {
        if (!latestPerKey)
        {
            return;
        }

        foreach (ColumnRef reference in model.DistinctOn)
        {
            ValidateColumn(catalog, model.BaseView, reference.Column);
        }

        List<string> keys = model
            .DistinctOn.Select(reference => $"\"{model.BaseView}\".\"{reference.Column}\"")
            .ToList();

        List<string> order = new List<string>(keys.Select(key => key + " ASC"));
        if (model.LatestBy is not null)
        {
            ValidateColumn(catalog, model.BaseView, model.LatestBy.Column);
            order.Add($"\"{model.BaseView}\".\"{model.LatestBy.Column}\" DESC");
        }

        sql.Append(
            CultureInfo.InvariantCulture,
            $"WITH \"__latest\" AS (SELECT DISTINCT ON ({string.Join(", ", keys)}) * FROM \"{model.BaseView}\" ORDER BY {string.Join(", ", order)}) "
        );
    }

    private static void AppendOrderBy(
        QueryModel model,
        ProjectionCatalog catalog,
        StringBuilder sql
    )
    {
        if (model.OrderBy.Count == 0)
        {
            return;
        }

        List<string> orderBy = new List<string>();
        foreach (SortColumn sort in model.OrderBy)
        {
            ValidateColumn(catalog, sort.View, sort.Column);
            string direction = sort.Direction == SortDirection.Desc ? "DESC" : "ASC";
            orderBy.Add($"\"{sort.View}\".\"{sort.Column}\" {direction}");
        }

        sql.Append(" ORDER BY ").Append(string.Join(", ", orderBy));
    }

    private static void AppendLimit(QueryModel model, StringBuilder sql)
    {
        if (!model.Limit.HasValue)
        {
            return;
        }

        int limit = Math.Clamp(model.Limit.Value, 1, QueryLimits.MaxRows);
        sql.Append(CultureInfo.InvariantCulture, $" LIMIT {limit}");
    }

    private static string BuildGroup(
        FilterGroup group,
        ProjectionCatalog catalog,
        List<SqlParameterSpec> parameters
    )
    {
        List<string> parts = new List<string>();
        foreach (FilterCondition condition in group.Conditions)
        {
            parts.Add(BuildCondition(condition, catalog, parameters));
        }

        foreach (FilterGroup nested in group.Groups)
        {
            string inner = BuildGroup(nested, catalog, parameters);
            if (!string.IsNullOrEmpty(inner))
            {
                parts.Add("(" + inner + ")");
            }
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        string separator = group.Combinator == FilterCombinator.Or ? " OR " : " AND ";
        return string.Join(separator, parts);
    }

    private static string BuildCondition(
        FilterCondition condition,
        ProjectionCatalog catalog,
        List<SqlParameterSpec> parameters
    )
    {
        if (!catalog.TryGetColumnType(condition.View, condition.Column, out string sqlType))
        {
            throw new QueryValidationException(
                $"Unknown column '{condition.View}.{condition.Column}'."
            );
        }

        string columnExpression = $"\"{condition.View}\".\"{condition.Column}\"";
        return condition.Operator switch
        {
            ConditionOperator.Eq =>
                $"{columnExpression} = {ValueParam(condition.Value, sqlType, parameters)}",
            ConditionOperator.Neq =>
                $"{columnExpression} <> {ValueParam(condition.Value, sqlType, parameters)}",
            ConditionOperator.Gt =>
                $"{columnExpression} > {ValueParam(condition.Value, sqlType, parameters)}",
            ConditionOperator.Lt =>
                $"{columnExpression} < {ValueParam(condition.Value, sqlType, parameters)}",
            ConditionOperator.Gte =>
                $"{columnExpression} >= {ValueParam(condition.Value, sqlType, parameters)}",
            ConditionOperator.Lte =>
                $"{columnExpression} <= {ValueParam(condition.Value, sqlType, parameters)}",
            ConditionOperator.Contains =>
                $"{columnExpression}::text ILIKE '%' || {ValueParam(condition.Value, "text", parameters)} || '%'",
            ConditionOperator.In =>
                $"{columnExpression} = ANY({ValuesParam(condition.Values, sqlType, parameters)})",
            ConditionOperator.IsNull => $"{columnExpression} IS NULL",
            ConditionOperator.IsNotNull => $"{columnExpression} IS NOT NULL",
            _ => throw new QueryValidationException("Unsupported operator."),
        };
    }

    private static string ValueParam(
        JsonNode? node,
        string sqlType,
        List<SqlParameterSpec> parameters
    )
    {
        if (node is null)
        {
            throw new QueryValidationException("A filter value is required.");
        }

        if (string.Equals(sqlType, "numeric", StringComparison.Ordinal))
        {
            return AddParam(parameters, SqlParameterType.Numeric, ToDecimal(node));
        }

        if (string.Equals(sqlType, "boolean", StringComparison.Ordinal))
        {
            return AddParam(parameters, SqlParameterType.Boolean, ToBool(node));
        }

        string placeholder = AddParam(parameters, SqlParameterType.Text, node.ToString());
        return NeedsCast(sqlType) ? $"{placeholder}::{sqlType}" : placeholder;
    }

    private static string ValuesParam(
        IReadOnlyList<JsonNode>? values,
        string sqlType,
        List<SqlParameterSpec> parameters
    )
    {
        if (values is null || values.Count == 0)
        {
            throw new QueryValidationException("At least one value is required for 'In'.");
        }

        if (string.Equals(sqlType, "numeric", StringComparison.Ordinal))
        {
            decimal[] numbers = values.Select(ToDecimal).ToArray();
            return AddParam(parameters, SqlParameterType.NumericArray, numbers);
        }

        string[] texts = values.Select(node => node.ToString()).ToArray();
        string placeholder = AddParam(parameters, SqlParameterType.TextArray, texts);
        return NeedsCast(sqlType) ? $"{placeholder}::{sqlType}[]" : placeholder;
    }

    private static bool NeedsCast(string sqlType)
    {
        return sqlType is "uuid" or "timestamptz" or "timestamp" or "date" or "time" or "timetz";
    }

    private static string AddParam(
        List<SqlParameterSpec> parameters,
        SqlParameterType type,
        object? value
    )
    {
        string name = "p" + parameters.Count.ToString(CultureInfo.InvariantCulture);
        parameters.Add(
            new SqlParameterSpec
            {
                Name = name,
                Type = type,
                Value = value,
            }
        );
        return "@" + name;
    }

    private static void ValidateColumn(ProjectionCatalog catalog, string view, string column)
    {
        if (!catalog.TryGetColumnType(view, column, out string _))
        {
            throw new QueryValidationException($"Unknown column '{view}.{column}'.");
        }
    }

    private static string ResolveAlias(SelectColumn column)
    {
        if (column.Alias is null)
        {
            return column.Column;
        }

        if (!AliasPattern.IsMatch(column.Alias))
        {
            throw new QueryValidationException($"Invalid column alias '{column.Alias}'.");
        }

        return column.Alias;
    }

    private static string AggregateName(AggregateFunction aggregate)
    {
        return aggregate switch
        {
            AggregateFunction.Count => "count",
            AggregateFunction.Sum => "sum",
            AggregateFunction.Avg => "avg",
            AggregateFunction.Min => "min",
            AggregateFunction.Max => "max",
            _ => throw new QueryValidationException("Unsupported aggregate."),
        };
    }

    private static decimal ToDecimal(JsonNode node)
    {
        try
        {
            return node.GetValue<decimal>();
        }
        catch (InvalidOperationException)
        {
            return ParseDecimal(node);
        }
        catch (FormatException)
        {
            return ParseDecimal(node);
        }
    }

    private static decimal ParseDecimal(JsonNode node)
    {
        if (
            decimal.TryParse(
                node.ToString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal parsed
            )
        )
        {
            return parsed;
        }

        throw new QueryValidationException("A numeric filter value was expected.");
    }

    private static bool ToBool(JsonNode node)
    {
        try
        {
            return node.GetValue<bool>();
        }
        catch (InvalidOperationException)
        {
            return ParseBool(node);
        }
        catch (FormatException)
        {
            return ParseBool(node);
        }
    }

    private static bool ParseBool(JsonNode node)
    {
        if (bool.TryParse(node.ToString(), out bool parsed))
        {
            return parsed;
        }

        throw new QueryValidationException("A boolean filter value was expected.");
    }
}
