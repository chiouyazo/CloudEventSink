using System.Text.Json;

namespace CloudEventSink.Core.Ingestion;

public static class DedupKeyResolver
{
    private const char Separator = (char)31;

    public static string? Resolve(string? dataJson, string? keyPaths)
    {
        if (string.IsNullOrWhiteSpace(keyPaths))
        {
            return null;
        }

        string[] paths = keyPaths.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (paths.Length == 0)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(dataJson) ? "null" : dataJson
        );
        List<string> values = new List<string>();
        foreach (string path in paths)
        {
            string? value = Extract(document.RootElement, path);
            if (value is null)
            {
                return null;
            }

            values.Add(value);
        }

        return string.Join(Separator, values);
    }

    private static string? Extract(JsonElement root, string path)
    {
        JsonElement current = root;
        foreach (string segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (
                current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out JsonElement next)
            )
            {
                return null;
            }

            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => current.GetRawText(),
        };
    }
}
