using CloudEventSink.Core.Schema;

namespace CloudEventSink.Web.Contracts;

public sealed record SchemaResponse
{
    public required string EventType { get; init; }

    public required long SampleCount { get; init; }

    public required DateTimeOffset FirstSeenUtc { get; init; }

    public required DateTimeOffset LastUpdatedUtc { get; init; }

    public required FieldNode Root { get; init; }
}
