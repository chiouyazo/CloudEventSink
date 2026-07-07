using System.Text.Json;

namespace CloudEventSink.Core.Projection;

public static class ProjectionSpecSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(ProjectionSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return JsonSerializer.Serialize(spec, Options);
    }

    public static ProjectionSpec Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<ProjectionSpec>(json, Options) ?? new ProjectionSpec();
    }
}
