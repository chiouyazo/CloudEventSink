namespace CloudEventSink.Core.Query;

public sealed record FilterGroup
{
    public FilterCombinator Combinator { get; init; } = FilterCombinator.And;

    public IReadOnlyList<FilterCondition> Conditions { get; init; } = [];

    public IReadOnlyList<FilterGroup> Groups { get; init; } = [];
}
