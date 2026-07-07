namespace CloudEventSink.Core.Query.Sql;

public sealed record GeneratedSql
{
    public required string CommandText { get; init; }

    public IReadOnlyList<SqlParameterSpec> Parameters { get; init; } = [];

    public IReadOnlyList<string> ResultColumns { get; init; } = [];
}
