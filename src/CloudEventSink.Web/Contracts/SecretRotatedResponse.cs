namespace CloudEventSink.Web.Contracts;

public sealed record SecretRotatedResponse
{
    public required Guid SourceId { get; init; }

    public required string SecretLastFour { get; init; }

    public required string PlaintextSecret { get; init; }
}
