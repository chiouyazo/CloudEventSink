using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudEventSink.Core.Schema;

public static class FieldNodeSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(FieldNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return JsonSerializer.Serialize(root, Options);
    }

    public static FieldNode Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        FieldNode? node = JsonSerializer.Deserialize<FieldNode>(json, Options);
        return node
            ?? throw new InvalidOperationException(
                "The stored field tree could not be deserialized."
            );
    }
}
