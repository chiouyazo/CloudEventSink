namespace CloudEventSink.Core.Abstractions;

public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public long TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}
