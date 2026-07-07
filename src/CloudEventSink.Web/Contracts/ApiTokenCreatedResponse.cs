namespace CloudEventSink.Web.Contracts;

public sealed record ApiTokenCreatedResponse
{
    public required ApiTokenResponse Token { get; init; }

    public required string PlaintextToken { get; init; }
}
