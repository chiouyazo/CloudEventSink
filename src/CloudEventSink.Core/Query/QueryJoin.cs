namespace CloudEventSink.Core.Query;

public sealed record QueryJoin
{
    public JoinType Type { get; init; } = JoinType.Left;

    public required string TargetView { get; init; }

    public required string LeftView { get; init; }

    public required string LeftColumn { get; init; }

    public required string RightView { get; init; }

    public required string RightColumn { get; init; }

    public IReadOnlyList<JoinKeyPair> Keys { get; init; } = [];
}
