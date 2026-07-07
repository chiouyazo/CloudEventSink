using CloudEventSink.Core.Entities;

namespace CloudEventSink.Core.Abstractions;

public interface ISavedQueryRepository
{
    Task<IReadOnlyList<SavedQuery>> ListAsync(CancellationToken cancellationToken);

    Task<SavedQuery?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(SavedQuery savedQuery);

    void Remove(SavedQuery savedQuery);
}
