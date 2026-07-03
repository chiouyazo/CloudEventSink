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

    Task<DateTimeOffset?> LatestReceivedAtAsync(Guid sourceId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid sourceId, string eventId, CancellationToken cancellationToken);

    void Add(EventRecord eventRecord);
}
