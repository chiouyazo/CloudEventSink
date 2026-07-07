namespace CloudEventSink.Core.Query;

public sealed record SortColumn
{
    public required string View { get; init; }

    public required string Column { get; init; }

    public SortDirection Direction { get; init; } = SortDirection.Asc;
}
