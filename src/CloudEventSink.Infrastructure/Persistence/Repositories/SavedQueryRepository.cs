using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence.Repositories;

public sealed class SavedQueryRepository : ISavedQueryRepository
{
    private readonly AppDbContext dbContext;

    public SavedQueryRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SavedQuery>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext
            .SavedQueries.AsNoTracking()
            .OrderBy(query => query.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<SavedQuery?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext
            .SavedQueries.FirstOrDefaultAsync(query => query.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(SavedQuery savedQuery)
    {
        dbContext.SavedQueries.Add(savedQuery);
    }

    public void Remove(SavedQuery savedQuery)
    {
        dbContext.SavedQueries.Remove(savedQuery);
    }
}
