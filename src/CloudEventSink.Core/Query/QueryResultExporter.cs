using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CloudEventSink.Core.Query;

public static class QueryResultExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    public static string ToCsv(QueryResultSet result)
    {
        ArgumentNullException.ThrowIfNull(result);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(string.Join(",", result.Columns.Select(column => Escape(column.Name))));

        foreach (IReadOnlyList<JsonNode?> row in result.Rows)
        {
            builder.AppendLine(string.Join(",", row.Select(cell => Escape(CellText(cell)))));
        }

        return builder.ToString();
    }

    public static string ToJson(QueryResultSet result)
    {
        ArgumentNullException.ThrowIfNull(result);

        JsonArray array = new JsonArray();
        foreach (IReadOnlyList<JsonNode?> row in result.Rows)
        {
            JsonObject item = new JsonObject();
            for (int index = 0; index < result.Columns.Count && index < row.Count; index++)
            {
                item[result.Columns[index].Name] = row[index]?.DeepClone();
            }

            array.Add(item);
        }

        return array.ToJsonString(JsonOptions);
    }

    private static string CellText(JsonNode? node)
    {
        return node?.ToString() ?? string.Empty;
    }

    private static string Escape(string value)
    {
        bool mustQuote =
            value.Contains(',', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal);

        if (!mustQuote)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
