using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence.Repositories;

public sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext dbContext;

    public EventRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<EventRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext
            .Events.AsNoTracking()
            .FirstOrDefaultAsync(record => record.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<EventRecord>> ListBySourceAsync(
        Guid sourceId,
        string? eventType,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        IQueryable<EventRecord> query = dbContext
            .Events.AsNoTracking()
            .Where(record => record.SourceId == sourceId);

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(record => record.EventType == eventType);
        }

        long total = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<EventRecord> items = await query
            .OrderByDescending(record => record.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<EventRecord>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<long> CountBySourceAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        return await dbContext
            .Events.AsNoTracking()
            .LongCountAsync(record => record.SourceId == sourceId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<long> CountReceivedBeforeAsync(
        Guid sourceId,
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Events.AsNoTracking()
            .LongCountAsync(
                record => record.SourceId == sourceId && record.ReceivedAtUtc < cutoffUtc,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<int> DeleteReceivedBeforeAsync(
        Guid sourceId,
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Events.Where(record => record.SourceId == sourceId && record.ReceivedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DateTimeOffset?> LatestReceivedAtAsync(
        Guid sourceId,
        CancellationToken cancellationToken
    )
    {
        List<DateTimeOffset> latest = await dbContext
            .Events.AsNoTracking()
            .Where(record => record.SourceId == sourceId)
            .OrderByDescending(record => record.ReceivedAtUtc)
            .Select(record => record.ReceivedAtUtc)
            .Take(1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return latest.Count == 0 ? null : latest[0];
    }

    public async Task<bool> ExistsAsync(
        Guid sourceId,
        string eventId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Events.AsNoTracking()
            .AnyAsync(
                record => record.SourceId == sourceId && record.EventId == eventId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<EventRecord?> GetByDedupKeyAsync(
        Guid sourceId,
        string dedupKey,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Events.FirstOrDefaultAsync(
                record => record.SourceId == sourceId && record.DedupKey == dedupKey,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<EventRecord?> GetLatestByGroupKeyAsync(
        Guid sourceId,
        string groupKey,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Events.AsNoTracking()
            .Where(record => record.SourceId == sourceId && record.GroupKey == groupKey)
            .OrderByDescending(record => record.ReceivedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(EventRecord eventRecord)
    {
        dbContext.Events.Add(eventRecord);
    }
}
