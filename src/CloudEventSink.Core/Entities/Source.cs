using CloudEventSink.Core.Enums;

namespace CloudEventSink.Core.Entities;

public sealed class Source
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public SourceAuthMode AuthMode { get; set; }

    public required string SecretHash { get; set; }

    public required string SecretLastFour { get; set; }

    public string? IpAllowlist { get; set; }

    public bool IsActive { get; set; }

    public IngestMode Mode { get; set; } = IngestMode.IgnoreDuplicateById;

    public string? DedupKeyPaths { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
