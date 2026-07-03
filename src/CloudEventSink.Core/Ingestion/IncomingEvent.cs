namespace CloudEventSink.Core.Ingestion;

public sealed record IncomingEvent
{
    public string? SpecVersion { get; init; }

    public required string EventType { get; init; }

    public required string EventId { get; init; }

    public string? EventSource { get; init; }

    public string? Subject { get; init; }

    public string? DataContentType { get; init; }

    public DateTimeOffset? TimeUtc { get; init; }

    public required string EnvelopeJson { get; init; }

    public required string DataJson { get; init; }
}
