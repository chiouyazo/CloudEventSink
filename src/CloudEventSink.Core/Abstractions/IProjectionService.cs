using CloudEventSink.Core.Projection;

namespace CloudEventSink.Core.Abstractions;

public interface IProjectionService
{
    Task<int> RegenerateAsync(Guid sourceId, CancellationToken cancellationToken);

    Task<ProjectionSpec?> GetSpecAsync(
        Guid sourceId,
        string eventType,
        CancellationToken cancellationToken
    );

    Task SaveSpecAsync(
        Guid sourceId,
        string eventType,
        ProjectionSpec spec,
        CancellationToken cancellationToken
    );

    Task ResetSpecAsync(Guid sourceId, string eventType, CancellationToken cancellationToken);
}
