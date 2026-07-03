namespace CloudEventSink.Web.Contracts;

public sealed record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required long TotalCount { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }
}
