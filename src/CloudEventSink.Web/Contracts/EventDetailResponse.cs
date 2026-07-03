using System.Text.Json;

namespace CloudEventSink.Web.Contracts;

public sealed record EventDetailResponse
{
    public required Guid Id { get; init; }

    public required Guid SourceId { get; init; }

    public string? SpecVersion { get; init; }

    public required string EventType { get; init; }

    public required string EventId { get; init; }

    public string? EventSource { get; init; }

    public string? Subject { get; init; }

    public string? DataContentType { get; init; }

    public DateTimeOffset? TimeUtc { get; init; }

    public required DateTimeOffset ReceivedAtUtc { get; init; }

    public required JsonElement Envelope { get; init; }

    public required JsonElement Data { get; init; }
}
