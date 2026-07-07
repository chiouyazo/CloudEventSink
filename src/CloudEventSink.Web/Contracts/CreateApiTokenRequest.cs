using System.ComponentModel.DataAnnotations;

namespace CloudEventSink.Web.Contracts;

public sealed record CreateApiTokenRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
