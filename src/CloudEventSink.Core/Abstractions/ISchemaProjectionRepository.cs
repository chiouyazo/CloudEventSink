using CloudEventSink.Core.Entities;

namespace CloudEventSink.Core.Abstractions;

public interface ISchemaProjectionRepository
{
    Task<IReadOnlyList<SchemaProjection>> ListBySourceAsync(
        Guid sourceId,
        CancellationToken cancellationToken
    );

    Task<SchemaProjection?> GetAsync(
        Guid sourceId,
        string eventType,
        CancellationToken cancellationToken
    );

    void Add(SchemaProjection projection);
}
