namespace CloudEventSink.Core.Query;

public sealed record ColumnRef
{
    public required string View { get; init; }

    public required string Column { get; init; }
}
