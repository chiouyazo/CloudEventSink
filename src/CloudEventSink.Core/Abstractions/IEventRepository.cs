using CloudEventSink.Core.Entities;

namespace CloudEventSink.Core.Abstractions;

public interface IEventRepository
{
    Task<EventRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<EventRecord>> ListBySourceAsync(
        Guid sourceId,
        string? eventType,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<long> CountBySourceAsync(Guid sourceId, CancellationToken cancellationToken);

    Task<long> CountReceivedBeforeAsync(
        Guid sourceId,
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken
    );

    Task<int> DeleteReceivedBeforeAsync(
        Guid sourceId,
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken
    );

    Task<DateTimeOffset?> LatestReceivedAtAsync(Guid sourceId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid sourceId, string eventId, CancellationToken cancellationToken);

    Task<EventRecord?> GetByDedupKeyAsync(
        Guid sourceId,
        string dedupKey,
        CancellationToken cancellationToken
    );

    Task<EventRecord?> GetLatestByGroupKeyAsync(
        Guid sourceId,
        string groupKey,
        CancellationToken cancellationToken
    );

    void Add(EventRecord eventRecord);
}
