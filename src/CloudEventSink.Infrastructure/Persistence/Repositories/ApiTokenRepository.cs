using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence.Repositories;

public sealed class ApiTokenRepository : IApiTokenRepository
{
    private readonly AppDbContext dbContext;

    public ApiTokenRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ApiToken>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext
            .ApiTokens.AsNoTracking()
            .OrderByDescending(token => token.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext
            .ApiTokens.FirstOrDefaultAsync(token => token.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiToken?> GetActiveByHashAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .ApiTokens.AsNoTracking()
            .FirstOrDefaultAsync(
                token =>
                    token.TokenHash == tokenHash
                    && token.RevokedAtUtc == null
                    && (token.ExpiresAtUtc == null || token.ExpiresAtUtc > nowUtc),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task TouchLastUsedAsync(
        Guid id,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        await dbContext
            .ApiTokens.Where(token => token.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.LastUsedAtUtc, nowUtc),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public void Add(ApiToken token)
    {
        dbContext.ApiTokens.Add(token);
    }
}
