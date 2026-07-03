using System.Text.Json;

namespace CloudEventSink.Core.Schema;

public interface ISchemaInferenceService
{
    FieldNode Derive(JsonElement data);

    FieldNode Merge(FieldNode existing, FieldNode incoming);

    FieldNode RecomputePresence(FieldNode root);
}
