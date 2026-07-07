using CloudEventSink.Core.Query.Sql;

namespace CloudEventSink.Core.Query;

public interface IQueryRunner
{
    Task<QueryResultSet> ExecuteAsync(
        string commandText,
        IReadOnlyList<SqlParameterSpec> parameters,
        CancellationToken cancellationToken
    );
}
