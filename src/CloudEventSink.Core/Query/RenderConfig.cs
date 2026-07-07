namespace CloudEventSink.Core.Query;

public sealed record RenderConfig
{
    public IReadOnlyList<string> Grouping { get; init; } = [];
}
