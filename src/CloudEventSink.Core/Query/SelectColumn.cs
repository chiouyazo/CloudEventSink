namespace CloudEventSink.Core.Query;

public sealed record SelectColumn
{
    public required string View { get; init; }

    public required string Column { get; init; }

    public AggregateFunction? Aggregate { get; init; }

    public string? Alias { get; init; }
}
