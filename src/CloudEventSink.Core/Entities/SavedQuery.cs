using CloudEventSink.Core.Query;

namespace CloudEventSink.Core.Entities;

public sealed class SavedQuery
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public Guid? FolderId { get; set; }

    public Guid? SourceId { get; set; }

    public QueryExecutionMode Mode { get; set; }

    public string? Sql { get; set; }

    public string? ModelJson { get; set; }

    public required string RenderConfigJson { get; set; }

    public required string CreatedBy { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
