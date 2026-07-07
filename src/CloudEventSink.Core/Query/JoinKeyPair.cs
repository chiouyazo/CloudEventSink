namespace CloudEventSink.Core.Query;

public sealed record JoinKeyPair
{
    public required string LeftColumn { get; init; }

    public required string RightColumn { get; init; }
}
