namespace CloudEventSink.Core.Query;

public sealed record QueryResultColumn
{
    public required string Name { get; init; }

    public required string DataType { get; init; }
}
