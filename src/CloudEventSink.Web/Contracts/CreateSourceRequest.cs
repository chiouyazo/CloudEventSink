using System.ComponentModel.DataAnnotations;
using CloudEventSink.Core.Enums;

namespace CloudEventSink.Web.Contracts;

public sealed record CreateSourceRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required]
    [RegularExpression("^[a-z0-9-]{1,120}$")]
    public required string Slug { get; init; }

    [Required]
    public required SourceAuthMode AuthMode { get; init; }

    [StringLength(2048)]
    public string? IpAllowlist { get; init; }

    public bool IsActive { get; init; } = true;
}
