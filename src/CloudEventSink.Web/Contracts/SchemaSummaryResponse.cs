namespace CloudEventSink.Web.Contracts;

public sealed record SchemaSummaryResponse
{
    public required string EventType { get; init; }

    public required long SampleCount { get; init; }

    public required DateTimeOffset FirstSeenUtc { get; init; }

    public required DateTimeOffset LastUpdatedUtc { get; init; }
}
