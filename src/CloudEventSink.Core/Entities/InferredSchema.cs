namespace CloudEventSink.Core.Entities;

public sealed class InferredSchema
{
    public Guid Id { get; set; }

    public Guid SourceId { get; set; }

    public required string EventType { get; set; }

    public required string RootNodeJson { get; set; }

    public long SampleCount { get; set; }

    public DateTimeOffset FirstSeenUtc { get; set; }

    public DateTimeOffset LastUpdatedUtc { get; set; }
}
