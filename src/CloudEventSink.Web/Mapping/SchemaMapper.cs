using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Schema;
using CloudEventSink.Web.Contracts;

namespace CloudEventSink.Web.Mapping;

public static class SchemaMapper
{
    public static SchemaSummaryResponse ToSummary(InferredSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        return new SchemaSummaryResponse
        {
            EventType = schema.EventType,
            SampleCount = schema.SampleCount,
            FirstSeenUtc = schema.FirstSeenUtc,
            LastUpdatedUtc = schema.LastUpdatedUtc,
        };
    }

    public static SchemaResponse ToResponse(InferredSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        return new SchemaResponse
        {
            EventType = schema.EventType,
            SampleCount = schema.SampleCount,
            FirstSeenUtc = schema.FirstSeenUtc,
            LastUpdatedUtc = schema.LastUpdatedUtc,
            Root = FieldNodeSerializer.Deserialize(schema.RootNodeJson),
        };
    }
}
