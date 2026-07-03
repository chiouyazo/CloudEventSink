namespace CloudEventSink.Web.Contracts;

public sealed record SourceCreatedResponse
{
    public required SourceResponse Source { get; init; }

    public required string PlaintextSecret { get; init; }
}
