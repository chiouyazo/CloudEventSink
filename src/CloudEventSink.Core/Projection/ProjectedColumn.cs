namespace CloudEventSink.Core.Projection;

public sealed record ProjectedColumn
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> Path { get; init; }

    public required string SqlType { get; init; }
}
