using System.Text.Json.Nodes;

namespace CloudEventSink.Core.Query;

public sealed record FilterCondition
{
    public required string View { get; init; }

    public required string Column { get; init; }

    public required ConditionOperator Operator { get; init; }

    public JsonNode? Value { get; init; }

    public IReadOnlyList<JsonNode>? Values { get; init; }
}
