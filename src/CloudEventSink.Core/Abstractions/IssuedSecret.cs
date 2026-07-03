namespace CloudEventSink.Core.Abstractions;

public sealed record IssuedSecret
{
    public required string PlaintextSecret { get; init; }

    public required string StoredValue { get; init; }

    public required string LastFour { get; init; }
}
