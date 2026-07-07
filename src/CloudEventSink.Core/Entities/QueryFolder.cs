namespace CloudEventSink.Core.Entities;

public sealed class QueryFolder
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Guid? ParentId { get; set; }

    public required string CreatedBy { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
