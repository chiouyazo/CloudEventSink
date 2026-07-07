using CloudEventSink.Core.Entities;

namespace CloudEventSink.Core.Abstractions;

public interface IQueryFolderRepository
{
    Task<IReadOnlyList<QueryFolder>> ListAsync(CancellationToken cancellationToken);

    Task<QueryFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(QueryFolder folder);

    void Remove(QueryFolder folder);
}
