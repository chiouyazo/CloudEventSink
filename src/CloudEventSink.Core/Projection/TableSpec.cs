namespace CloudEventSink.Core.Projection;

public sealed record TableSpec
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public bool IsChild { get; init; }

    public bool ScalarArray { get; init; }

    public IReadOnlyList<string> Path { get; init; } = [];

    public string? ParentKey { get; init; }

    public ArrayMode Mode { get; init; } = ArrayMode.OwnTable;

    public IReadOnlyList<ColumnSpec> Columns { get; init; } = [];
}
