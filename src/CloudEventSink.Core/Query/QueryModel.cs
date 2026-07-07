namespace CloudEventSink.Core.Query;

public sealed record QueryModel
{
    public required string BaseView { get; init; }

    public IReadOnlyList<SelectColumn> Columns { get; init; } = [];

    public IReadOnlyList<QueryJoin> Joins { get; init; } = [];

    public IReadOnlyList<ColumnRef> DistinctOn { get; init; } = [];

    public ColumnRef? LatestBy { get; init; }

    public FilterGroup? Filters { get; init; }

    public IReadOnlyList<ColumnRef> GroupBy { get; init; } = [];

    public FilterGroup? Having { get; init; }

    public IReadOnlyList<SortColumn> OrderBy { get; init; } = [];

    public int? Limit { get; init; }
}
