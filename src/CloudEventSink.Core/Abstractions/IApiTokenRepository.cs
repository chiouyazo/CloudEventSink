using CloudEventSink.Core.Entities;

namespace CloudEventSink.Core.Abstractions;

public interface IApiTokenRepository
{
    Task<IReadOnlyList<ApiToken>> ListAsync(CancellationToken cancellationToken);

    Task<ApiToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ApiToken?> GetActiveByHashAsync(
        string tokenHash,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    );

    Task TouchLastUsedAsync(Guid id, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    void Add(ApiToken token);
}
