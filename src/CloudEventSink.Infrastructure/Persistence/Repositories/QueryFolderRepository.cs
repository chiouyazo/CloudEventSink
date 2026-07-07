using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence.Repositories;

public sealed class QueryFolderRepository : IQueryFolderRepository
{
    private readonly AppDbContext dbContext;

    public QueryFolderRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<QueryFolder>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext
            .QueryFolders.AsNoTracking()
            .OrderBy(folder => folder.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<QueryFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext
            .QueryFolders.FirstOrDefaultAsync(folder => folder.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(QueryFolder folder)
    {
        dbContext.QueryFolders.Add(folder);
    }

    public void Remove(QueryFolder folder)
    {
        dbContext.QueryFolders.Remove(folder);
    }
}
