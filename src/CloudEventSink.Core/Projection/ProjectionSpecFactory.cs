using System.Globalization;
using CloudEventSink.Core.Enums;
using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Projection;

public static class ProjectionSpecFactory
{
    public const string MainKey = "main";

    public static ProjectionSpec BuildDefault(
        string sourceSlug,
        string eventType,
        FieldNode dataRoot
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(dataRoot);

        List<TableSpec> tables = new List<TableSpec>();

        List<ColumnSpec> mainColumns = new List<ColumnSpec>
        {
            new ColumnSpec
            {
                Name = "event_id",
                Role = ColumnRole.EventId,
                SqlType = "uuid",
            },
            new ColumnSpec
            {
                Name = "received_at",
                Role = ColumnRole.ReceivedAt,
                SqlType = "timestamptz",
            },
            new ColumnSpec
            {
                Name = "time",
                Role = ColumnRole.EventTime,
                SqlType = "timestamptz",
            },
        };
        HashSet<string> mainUsed = new HashSet<string>(StringComparer.Ordinal)
        {
            "event_id",
            "received_at",
            "time",
        };
        List<(
            string Key,
            FieldNode Node,
            IReadOnlyList<string> AbsPath,
            string ParentKey,
            IReadOnlyList<ColumnSpec> ParentOrdinals
        )> pending =
            new List<(
                string,
                FieldNode,
                IReadOnlyList<string>,
                string,
                IReadOnlyList<ColumnSpec>
            )>();

        CollectScalars(dataRoot, [], [], mainColumns, mainUsed, MainKey, [], pending);

        tables.Add(
            new TableSpec
            {
                Key = MainKey,
                Name = ProjectionNaming.MainViewName(sourceSlug, eventType),
                IsChild = false,
                Path = [],
                Columns = mainColumns,
            }
        );

        int cursor = 0;
        while (cursor < pending.Count)
        {
            (
                string key,
                FieldNode node,
                IReadOnlyList<string> absPath,
                string parentKey,
                IReadOnlyList<ColumnSpec> parentOrdinals
            ) = pending[cursor];
            cursor++;

            FieldNode element = node.Element!;
            bool scalarArray = !IsObject(element);

            List<ColumnSpec> columns = new List<ColumnSpec>
            {
                new ColumnSpec
                {
                    Name = "event_id",
                    Role = ColumnRole.EventId,
                    SqlType = "uuid",
                },
            };
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal) { "event_id" };

            foreach (ColumnSpec inherited in parentOrdinals)
            {
                columns.Add(inherited);
                used.Add(inherited.Name);
            }

            string ownOrdinalName = Dedup($"{ProjectionNaming.ToSnake(absPath[^1])}_index", used);
            ColumnSpec ownOrdinal = new ColumnSpec
            {
                Name = ownOrdinalName,
                Role = ColumnRole.Ordinal,
                SqlType = "bigint",
            };
            columns.Add(ownOrdinal);

            List<ColumnSpec> ordinalsForChildren = new List<ColumnSpec>(parentOrdinals)
            {
                ownOrdinal,
            };

            if (scalarArray)
            {
                columns.Add(
                    new ColumnSpec
                    {
                        Name = Dedup("value", used),
                        SourcePath = [],
                        SqlType = SqlType(element),
                    }
                );
            }
            else
            {
                CollectScalars(
                    element,
                    [],
                    absPath,
                    columns,
                    used,
                    key,
                    ordinalsForChildren,
                    pending
                );
            }

            tables.Add(
                new TableSpec
                {
                    Key = key,
                    Name = ProjectionNaming.ChildViewName(sourceSlug, eventType, absPath),
                    IsChild = true,
                    ScalarArray = scalarArray,
                    Path = absPath,
                    ParentKey = parentKey,
                    Mode = ArrayMode.OwnTable,
                    Columns = columns,
                }
            );
        }

        return new ProjectionSpec { Tables = tables };
    }

    private static void CollectScalars(
        FieldNode node,
        IReadOnlyList<string> relativePath,
        IReadOnlyList<string> absolutePrefix,
        List<ColumnSpec> columns,
        HashSet<string> used,
        string parentKey,
        IReadOnlyList<ColumnSpec> parentOrdinals,
        List<(string, FieldNode, IReadOnlyList<string>, string, IReadOnlyList<ColumnSpec>)> pending
    )
    {
        foreach (FieldNode child in node.Children)
        {
            List<string> childRelative = new List<string>(relativePath) { child.Name };
            List<string> childAbsolute = new List<string>(absolutePrefix);
            childAbsolute.AddRange(childRelative);

            if (IsArray(child) && child.Element is not null)
            {
                pending.Add(
                    (
                        string.Join(".", childAbsolute),
                        child,
                        childAbsolute,
                        parentKey,
                        parentOrdinals
                    )
                );
            }
            else if (IsObject(child) && child.Children.Count > 0)
            {
                CollectScalars(
                    child,
                    childRelative,
                    absolutePrefix,
                    columns,
                    used,
                    parentKey,
                    parentOrdinals,
                    pending
                );
            }
            else
            {
                string name = Dedup(
                    string.Join("_", childRelative.Select(ProjectionNaming.ToSnake)),
                    used
                );
                columns.Add(
                    new ColumnSpec
                    {
                        Name = name,
                        SourcePath = childRelative,
                        SqlType = SqlType(child),
                        Role = ColumnRole.Scalar,
                    }
                );
            }
        }
    }

    private static string Dedup(string baseName, HashSet<string> used)
    {
        string candidate = baseName;
        int suffix = 2;
        while (!used.Add(candidate))
        {
            candidate = $"{baseName}_{suffix.ToString(CultureInfo.InvariantCulture)}";
            suffix++;
        }

        return candidate;
    }

    private static bool IsArray(FieldNode node)
    {
        return node.Kinds.Contains(JsonNodeKind.Array);
    }

    private static bool IsObject(FieldNode node)
    {
        return node.Kinds.Contains(JsonNodeKind.Object);
    }

    private static string SqlType(FieldNode node)
    {
        bool numeric =
            node.Kinds.Count > 0
            && node.Kinds.All(kind => kind is JsonNodeKind.Number or JsonNodeKind.Null)
            && node.Kinds.Contains(JsonNodeKind.Number);
        if (numeric)
        {
            return "numeric";
        }

        bool boolean =
            node.Kinds.Count > 0
            && node.Kinds.All(kind => kind is JsonNodeKind.Boolean or JsonNodeKind.Null)
            && node.Kinds.Contains(JsonNodeKind.Boolean);
        return boolean ? "boolean" : "text";
    }
}
