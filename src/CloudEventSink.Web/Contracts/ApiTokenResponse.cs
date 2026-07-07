namespace CloudEventSink.Web.Contracts;

public sealed record ApiTokenResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string TokenLastFour { get; init; }

    public DateTimeOffset? ExpiresAtUtc { get; init; }

    public DateTimeOffset? RevokedAtUtc { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? LastUsedAtUtc { get; init; }
}
