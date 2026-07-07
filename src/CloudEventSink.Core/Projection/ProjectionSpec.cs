namespace CloudEventSink.Core.Projection;

public sealed record ProjectionSpec
{
    public IReadOnlyList<TableSpec> Tables { get; init; } = [];
}
