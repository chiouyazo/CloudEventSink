using System.Text.Json.Nodes;

namespace CloudEventSink.Web.Contracts;

public sealed record QueryExecuteResponse
{
    public required IReadOnlyList<QueryColumnDto> Columns { get; init; }

    public required IReadOnlyList<IReadOnlyList<JsonNode?>> Rows { get; init; }

    public required bool Truncated { get; init; }
}
