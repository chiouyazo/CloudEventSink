using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence.Repositories;

public sealed class SourceRepository : ISourceRepository
{
    private readonly AppDbContext dbContext;

    public SourceRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Source>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext
            .Sources.AsNoTracking()
            .OrderBy(source => source.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Source?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext
            .Sources.FirstOrDefaultAsync(source => source.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Source?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await dbContext
            .Sources.FirstOrDefaultAsync(source => source.Slug == slug, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludingId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Sources.AnyAsync(
                source => source.Slug == slug && (excludingId == null || source.Id != excludingId),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .Sources.AnyAsync(
                source => source.Name == name && (excludingId == null || source.Id != excludingId),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public void Add(Source source)
    {
        dbContext.Sources.Add(source);
    }

    public void Remove(Source source)
    {
        dbContext.Sources.Remove(source);
    }
}
