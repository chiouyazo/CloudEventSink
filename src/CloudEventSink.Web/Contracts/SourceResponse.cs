using CloudEventSink.Core.Enums;

namespace CloudEventSink.Web.Contracts;

public sealed record SourceResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required SourceAuthMode AuthMode { get; init; }

    public required string SecretLastFour { get; init; }

    public string? IpAllowlist { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required DateTimeOffset UpdatedAtUtc { get; init; }
}
