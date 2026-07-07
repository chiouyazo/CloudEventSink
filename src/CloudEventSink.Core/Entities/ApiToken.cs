namespace CloudEventSink.Core.Entities;

public sealed class ApiToken
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string TokenHash { get; set; }

    public required string TokenLastFour { get; set; }

    public required string CreatedBy { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastUsedAtUtc { get; set; }
}
