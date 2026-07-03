using CloudEventSink.Core.Entities;

namespace CloudEventSink.Core.Abstractions;

public interface ISchemaRepository
{
    Task<InferredSchema?> GetAsync(
        Guid sourceId,
        string eventType,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<InferredSchema>> ListBySourceAsync(
        Guid sourceId,
        CancellationToken cancellationToken
    );

    void Add(InferredSchema schema);
}
