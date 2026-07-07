using System.Text.Json;

namespace CloudEventSink.Core.Projection;

public static class ProjectionSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string SerializeColumns(IReadOnlyList<ProjectedColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        return JsonSerializer.Serialize(columns, Options);
    }

    public static IReadOnlyList<ProjectedColumn> DeserializeColumns(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<List<ProjectedColumn>>(json, Options) ?? [];
    }

    public static string SerializeViews(IReadOnlyList<ProjectedView> views)
    {
        ArgumentNullException.ThrowIfNull(views);
        return JsonSerializer.Serialize(views, Options);
    }

    public static IReadOnlyList<ProjectedView> DeserializeViews(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<List<ProjectedView>>(json, Options) ?? [];
    }
}
