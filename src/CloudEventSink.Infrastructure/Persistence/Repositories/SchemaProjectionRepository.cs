using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence.Repositories;

public sealed class SchemaProjectionRepository : ISchemaProjectionRepository
{
    private readonly AppDbContext dbContext;

    public SchemaProjectionRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SchemaProjection>> ListBySourceAsync(
        Guid sourceId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .SchemaProjections.AsNoTracking()
            .Where(projection => projection.SourceId == sourceId)
            .OrderBy(projection => projection.EventType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<SchemaProjection?> GetAsync(
        Guid sourceId,
        string eventType,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .SchemaProjections.FirstOrDefaultAsync(
                projection => projection.SourceId == sourceId && projection.EventType == eventType,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public void Add(SchemaProjection projection)
    {
        dbContext.SchemaProjections.Add(projection);
    }
}
