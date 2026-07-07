using CloudEventSink.Core.Query.Sql;

namespace CloudEventSink.Core.Query;

public interface IQueryModelCompiler
{
    GeneratedSql Compile(QueryModel model, ProjectionCatalog catalog);
}
