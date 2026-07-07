namespace CloudEventSink.Core.Projection;

public static class ProjectionSpecMerger
{
    public static ProjectionSpec Merge(ProjectionSpec existing, ProjectionSpec fresh)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(fresh);

        Dictionary<string, TableSpec> existingByKey = existing.Tables.ToDictionary(
            table => table.Key,
            StringComparer.Ordinal
        );
        List<TableSpec> merged = new List<TableSpec>();
        HashSet<string> processed = new HashSet<string>(StringComparer.Ordinal);

        foreach (TableSpec freshTable in fresh.Tables)
        {
            processed.Add(freshTable.Key);
            if (existingByKey.TryGetValue(freshTable.Key, out TableSpec? existingTable))
            {
                merged.Add(
                    freshTable with
                    {
                        Name = existingTable.Name,
                        Mode = existingTable.Mode,
                        Columns = MergeColumns(existingTable.Columns, freshTable.Columns),
                    }
                );
            }
            else
            {
                merged.Add(freshTable);
            }
        }

        merged.AddRange(existing.Tables.Where(table => !processed.Contains(table.Key)));

        return new ProjectionSpec { Tables = merged };
    }

    private static List<ColumnSpec> MergeColumns(
        IReadOnlyList<ColumnSpec> existing,
        IReadOnlyList<ColumnSpec> fresh
    )
    {
        HashSet<string> existingIds = new HashSet<string>(
            Identities(existing).Select(entry => entry.Id),
            StringComparer.Ordinal
        );
        List<ColumnSpec> merged = new List<ColumnSpec>(existing);
        merged.AddRange(
            Identities(fresh)
                .Where(entry => !existingIds.Contains(entry.Id))
                .Select(entry => entry.Column)
        );
        return merged;
    }

    private static IEnumerable<(string Id, ColumnSpec Column)> Identities(
        IReadOnlyList<ColumnSpec> columns
    )
    {
        int ordinal = 0;
        foreach (ColumnSpec column in columns)
        {
            string id = column.Role switch
            {
                ColumnRole.EventId => "r:eventid",
                ColumnRole.ReceivedAt => "r:receivedat",
                ColumnRole.EventTime => "r:eventtime",
                ColumnRole.Ordinal => "r:ordinal:"
                    + ordinal++.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => "p:" + string.Join(".", column.SourcePath),
            };
            yield return (id, column);
        }
    }
}
