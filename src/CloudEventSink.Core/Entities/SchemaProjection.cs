namespace CloudEventSink.Core.Entities;

public sealed class SchemaProjection
{
    public Guid Id { get; set; }

    public Guid SourceId { get; set; }

    public required string EventType { get; set; }

    public required string MainViewName { get; set; }

    public required string ColumnsJson { get; set; }

    public required string ChildViewsJson { get; set; }

    public string? SpecJson { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }
}
