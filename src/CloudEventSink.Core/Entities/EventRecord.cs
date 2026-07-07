namespace CloudEventSink.Core.Entities;

public sealed class EventRecord
{
    public Guid Id { get; set; }

    public Guid SourceId { get; set; }

    public string? SpecVersion { get; set; }

    public required string EventType { get; set; }

    public required string EventId { get; set; }

    public string? DedupKey { get; set; }

    public string? GroupKey { get; set; }

    public string? EventSource { get; set; }

    public string? Subject { get; set; }

    public string? DataContentType { get; set; }

    public DateTimeOffset? TimeUtc { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    public required string Envelope { get; set; }

    public required string Data { get; set; }
}
