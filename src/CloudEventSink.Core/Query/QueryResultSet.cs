using System.Text.Json.Nodes;

namespace CloudEventSink.Core.Query;

public sealed record QueryResultSet
{
    public IReadOnlyList<QueryResultColumn> Columns { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<JsonNode?>> Rows { get; init; } = [];

    public bool Truncated { get; init; }
}
