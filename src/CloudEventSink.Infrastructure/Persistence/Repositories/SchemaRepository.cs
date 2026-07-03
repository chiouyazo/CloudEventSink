using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence.Repositories;

public sealed class SchemaRepository : ISchemaRepository
{
    private readonly AppDbContext dbContext;

    public SchemaRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<InferredSchema?> GetAsync(
        Guid sourceId,
        string eventType,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Schemas.FirstOrDefaultAsync(
                schema => schema.SourceId == sourceId && schema.EventType == eventType,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<InferredSchema>> ListBySourceAsync(
        Guid sourceId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Schemas.AsNoTracking()
            .Where(schema => schema.SourceId == sourceId)
            .OrderBy(schema => schema.EventType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(InferredSchema schema)
    {
        dbContext.Schemas.Add(schema);
    }
}
