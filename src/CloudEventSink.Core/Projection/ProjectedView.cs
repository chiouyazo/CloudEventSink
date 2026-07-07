namespace CloudEventSink.Core.Projection;

public sealed record ProjectedView
{
    public required string Name { get; init; }

    public required bool IsChild { get; init; }

    public bool ScalarArray { get; init; }

    public IReadOnlyList<string> ArrayPath { get; init; } = [];

    public required IReadOnlyList<ProjectedColumn> Columns { get; init; }
}
