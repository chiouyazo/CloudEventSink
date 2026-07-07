using System.Globalization;
using System.Text;
using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Projection;

public sealed class ProjectionGenerator : IProjectionGenerator
{
    private static readonly HashSet<string> AllowedTypes = new HashSet<string>(
        StringComparer.Ordinal
    )
    {
        "text",
        "numeric",
        "boolean",
        "uuid",
        "timestamptz",
        "timestamp",
        "date",
        "jsonb",
        "bigint",
        "integer",
    };

    public ProjectionPlan BuildDefaultPlan(
        Guid sourceId,
        string sourceSlug,
        string eventType,
        FieldNode dataRoot
    )
    {
        ProjectionSpec spec = ProjectionSpecFactory.BuildDefault(sourceSlug, eventType, dataRoot);
        return this.Generate(sourceId, eventType, spec);
    }

    public ProjectionPlan Generate(Guid sourceId, string eventType, ProjectionSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        Dictionary<string, TableSpec> byKey = spec.Tables.ToDictionary(
            table => table.Key,
            StringComparer.Ordinal
        );
        TableSpec main = spec.Tables.First(table => !table.IsChild);

        List<ProjectedView> materialized = new List<ProjectedView>();
        StringBuilder create = new StringBuilder();
        StringBuilder drop = new StringBuilder();

        List<TableSpec> ordered = spec
            .Tables.Where(table =>
                !table.IsChild
                || (table.Mode == ArrayMode.OwnTable && ChainIsOwnTable(table, byKey))
            )
            .ToList();

        foreach (TableSpec table in ordered)
        {
            ProjectedView view = BuildView(table, byKey);
            materialized.Add(view);
            if (create.Length > 0)
            {
                create.Append('\n');
            }

            create.Append(BuildViewSql(sourceId, eventType, table, byKey));
        }

        foreach (ProjectedView child in materialized.Where(view => view.IsChild))
        {
            drop.Append(
                CultureInfo.InvariantCulture,
                $"DROP VIEW IF EXISTS \"{Identifier(child.Name)}\" CASCADE;\n"
            );
        }

        drop.Append(
            CultureInfo.InvariantCulture,
            $"DROP VIEW IF EXISTS \"{Identifier(main.Name)}\" CASCADE;"
        );

        ProjectedView mainView = materialized.First(view => !view.IsChild);
        return new ProjectionPlan
        {
            MainView = mainView,
            ChildViews = materialized.Where(view => view.IsChild).ToList(),
            CreateSql = create.ToString(),
            DropSql = drop.ToString(),
        };
    }

    private static bool ChainIsOwnTable(TableSpec table, Dictionary<string, TableSpec> byKey)
    {
        TableSpec current = table;
        while (current.IsChild)
        {
            if (current.Mode != ArrayMode.OwnTable)
            {
                return false;
            }

            if (
                current.ParentKey is null
                || !byKey.TryGetValue(current.ParentKey, out TableSpec? parent)
            )
            {
                return false;
            }

            current = parent;
        }

        return true;
    }

    private static List<TableSpec> ArrayChain(TableSpec table, Dictionary<string, TableSpec> byKey)
    {
        List<TableSpec> chain = new List<TableSpec>();
        TableSpec current = table;
        while (current.IsChild)
        {
            chain.Add(current);
            current = byKey[current.ParentKey!];
        }

        chain.Reverse();
        return chain;
    }

    private static ProjectedView BuildView(TableSpec table, Dictionary<string, TableSpec> byKey)
    {
        List<ProjectedColumn> columns = new List<ProjectedColumn>();
        foreach (ColumnSpec column in table.Columns.Where(column => column.Included))
        {
            columns.Add(
                new ProjectedColumn
                {
                    Name = column.Name,
                    Path = column.SourcePath,
                    SqlType = EffectiveType(column),
                }
            );
        }

        foreach (TableSpec jsonChild in JsonColumnChildren(table, byKey))
        {
            columns.Add(
                new ProjectedColumn
                {
                    Name = jsonChild.Name,
                    Path = jsonChild.Path,
                    SqlType = "jsonb",
                }
            );
        }

        return new ProjectedView
        {
            Name = table.Name,
            IsChild = table.IsChild,
            ScalarArray = table.ScalarArray,
            ArrayPath = table.Path,
            Columns = columns,
        };
    }

    private static IEnumerable<TableSpec> JsonColumnChildren(
        TableSpec table,
        Dictionary<string, TableSpec> byKey
    )
    {
        return byKey.Values.Where(candidate =>
            candidate.IsChild
            && candidate.Mode == ArrayMode.JsonColumn
            && string.Equals(candidate.ParentKey, table.Key, StringComparison.Ordinal)
        );
    }

    private static string BuildViewSql(
        Guid sourceId,
        string eventType,
        TableSpec table,
        Dictionary<string, TableSpec> byKey
    )
    {
        List<TableSpec> chain = ArrayChain(table, byKey);
        string elementRoot =
            chain.Count == 0
                ? "e.\"Data\""
                : $"a{chain.Count.ToString(CultureInfo.InvariantCulture)}.value";

        List<string> selects = new List<string>();
        int ordinalIndex = 0;
        foreach (ColumnSpec column in table.Columns.Where(column => column.Included))
        {
            selects.Add(
                $"{ColumnExpression(table, column, elementRoot, ref ordinalIndex)} AS \"{Identifier(column.Name)}\""
            );
        }

        foreach (TableSpec jsonChild in JsonColumnChildren(table, byKey))
        {
            IReadOnlyList<string> relative = jsonChild.Path.Skip(table.Path.Count).ToList();
            selects.Add(
                $"({elementRoot} #> {PathArray(relative)}) AS \"{Identifier(jsonChild.Name)}\""
            );
        }

        StringBuilder from = new StringBuilder("\"events\" e");
        for (int level = 0; level < chain.Count; level++)
        {
            TableSpec arrayTable = chain[level];
            string parentRoot =
                level == 0
                    ? "e.\"Data\""
                    : $"a{level.ToString(CultureInfo.InvariantCulture)}.value";
            IReadOnlyList<string> parentPath = level == 0 ? [] : chain[level - 1].Path;
            IReadOnlyList<string> relative = arrayTable.Path.Skip(parentPath.Count).ToList();
            string function = arrayTable.ScalarArray
                ? "jsonb_array_elements_text"
                : "jsonb_array_elements";
            int alias = level + 1;
            from.Append(
                CultureInfo.InvariantCulture,
                $"\n  CROSS JOIN LATERAL {function}({parentRoot} #> {PathArray(relative)}) WITH ORDINALITY AS a{alias.ToString(CultureInfo.InvariantCulture)}(value, ord)"
            );
        }

        string where =
            $"e.\"SourceId\" = '{sourceId}'::uuid AND e.\"EventType\" = {Literal(eventType)}";
        string selectList = string.Join(",\n  ", selects);
        return $"CREATE OR REPLACE VIEW \"{Identifier(table.Name)}\" AS\nSELECT\n  {selectList}\nFROM {from}\nWHERE {where};";
    }

    private static string ColumnExpression(
        TableSpec table,
        ColumnSpec column,
        string elementRoot,
        ref int ordinalIndex
    )
    {
        switch (column.Role)
        {
            case ColumnRole.EventId:
                return "e.\"Id\"";
            case ColumnRole.ReceivedAt:
                return "e.\"ReceivedAtUtc\"";
            case ColumnRole.EventTime:
                return "e.\"TimeUtc\"";
            case ColumnRole.Ordinal:
                ordinalIndex++;
                return $"a{ordinalIndex.ToString(CultureInfo.InvariantCulture)}.ord";
            default:
                break;
        }

        if (table.ScalarArray && column.SourcePath.Count == 0)
        {
            return Cast(elementRoot, SafeType(column.SqlType));
        }

        return Cast(
            $"({elementRoot} #>> {PathArray(column.SourcePath)})",
            SafeType(column.SqlType)
        );
    }

    private static string EffectiveType(ColumnSpec column)
    {
        return column.Role switch
        {
            ColumnRole.EventId => "uuid",
            ColumnRole.ReceivedAt => "timestamptz",
            ColumnRole.EventTime => "timestamptz",
            ColumnRole.Ordinal => "bigint",
            _ => SafeType(column.SqlType),
        };
    }

    private static string Cast(string expression, string sqlType)
    {
        return string.Equals(sqlType, "text", StringComparison.Ordinal)
            ? expression
            : $"(NULLIF({expression}, ''))::{sqlType}";
    }

    private static string SafeType(string sqlType)
    {
        return AllowedTypes.Contains(sqlType) ? sqlType : "text";
    }

    private static string PathArray(IReadOnlyList<string> path)
    {
        if (path.Count == 0)
        {
            return "ARRAY[]::text[]";
        }

        return "ARRAY[" + string.Join(",", path.Select(Literal)) + "]";
    }

    private static string Literal(string value)
    {
        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static string Identifier(string name)
    {
        string escaped = string.IsNullOrWhiteSpace(name)
            ? "column"
            : name.Replace("\"", "\"\"", StringComparison.Ordinal);
        return escaped.Length <= 63 ? escaped : escaped[..63];
    }
}
