using CloudEventSink.Core.Entities;

namespace CloudEventSink.Core.Abstractions;

public interface ISourceRepository
{
    Task<IReadOnlyList<Source>> ListAsync(CancellationToken cancellationToken);

    Task<Source?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Source?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken);

    void Add(Source source);

    void Remove(Source source);
}
