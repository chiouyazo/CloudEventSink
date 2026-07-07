using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Projection;

public interface IProjectionGenerator
{
    ProjectionPlan Generate(Guid sourceId, string eventType, ProjectionSpec spec);

    ProjectionPlan BuildDefaultPlan(
        Guid sourceId,
        string sourceSlug,
        string eventType,
        FieldNode dataRoot
    );
}
