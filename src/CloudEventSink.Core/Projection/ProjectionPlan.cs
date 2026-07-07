namespace CloudEventSink.Core.Projection;

public sealed record ProjectionPlan
{
    public required ProjectedView MainView { get; init; }

    public IReadOnlyList<ProjectedView> ChildViews { get; init; } = [];

    public required string CreateSql { get; init; }

    public required string DropSql { get; init; }
}
