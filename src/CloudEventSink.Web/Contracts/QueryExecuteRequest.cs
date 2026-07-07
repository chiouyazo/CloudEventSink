using CloudEventSink.Core.Query;

namespace CloudEventSink.Web.Contracts;

public sealed record QueryExecuteRequest
{
    public QueryExecutionMode Mode { get; init; } = QueryExecutionMode.Visual;

    public Guid? SourceId { get; init; }

    public string? Sql { get; init; }

    public QueryModel? Model { get; init; }
}
