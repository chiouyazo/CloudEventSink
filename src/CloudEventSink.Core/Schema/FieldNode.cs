using CloudEventSink.Core.Enums;

namespace CloudEventSink.Core.Schema;

public sealed record FieldNode
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<JsonNodeKind> Kinds { get; init; } = [];

    public bool Nullable { get; init; }

    public long SeenCount { get; init; }

    public double PresenceRatio { get; init; }

    public IReadOnlyList<FieldNode> Children { get; init; } = [];

    public FieldNode? Element { get; init; }
}
