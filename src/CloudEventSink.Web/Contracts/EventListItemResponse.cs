namespace CloudEventSink.Web.Contracts;

public sealed record EventListItemResponse
{
    public required Guid Id { get; init; }

    public string? SpecVersion { get; init; }

    public required string EventType { get; init; }

    public required string EventId { get; init; }

    public string? EventSource { get; init; }

    public string? Subject { get; init; }

    public DateTimeOffset? TimeUtc { get; init; }

    public required DateTimeOffset ReceivedAtUtc { get; init; }
}
