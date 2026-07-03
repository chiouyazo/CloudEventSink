using System.ComponentModel.DataAnnotations;

namespace CloudEventSink.Web.Contracts;

public sealed record UpdateSourceRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required]
    [RegularExpression("^[a-z0-9-]{1,120}$")]
    public required string Slug { get; init; }

    [StringLength(2048)]
    public string? IpAllowlist { get; init; }

    public bool IsActive { get; init; }
}
