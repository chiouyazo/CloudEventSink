using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Projection;
using CloudEventSink.Core.Query;
using CloudEventSink.Core.Query.Sql;
using CloudEventSink.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CloudEventSink.Web.Api;

public static class QueryEndpoints
{
    public static IEndpointRouteBuilder MapQueryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost("/api/query/execute", ExecuteAsync)
            .RequireAuthorization(Auth.AuthorizationPolicies.QueryAccess)
            .DisableAntiforgery()
            .WithTags("Query");

        return endpoints;
    }

    [EndpointSummary(
        "Executes a read-only SQL statement or a visual query model against the projected views."
    )]
    [ProducesResponseType<QueryExecuteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    private static async Task<IResult> ExecuteAsync(
        QueryExecuteRequest request,
        ISchemaProjectionRepository projections,
        IQueryModelCompiler compiler,
        IQueryRunner runner,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            QueryResultSet result =
                request.Mode == QueryExecutionMode.Sql
                    ? await ExecuteSqlAsync(request, runner, cancellationToken)
                        .ConfigureAwait(false)
                    : await ExecuteModelAsync(
                            request,
                            projections,
                            compiler,
                            runner,
                            cancellationToken
                        )
                        .ConfigureAwait(false);

            return Results.Ok(Map(result));
        }
        catch (QueryValidationException exception)
        {
            return Results.BadRequest(new ErrorResponse { Error = exception.Message });
        }
        catch (PostgresException exception)
        {
            return Results.BadRequest(
                new ErrorResponse { Error = $"Query failed: {exception.MessageText}" }
            );
        }
    }

    private static async Task<QueryResultSet> ExecuteSqlAsync(
        QueryExecuteRequest request,
        IQueryRunner runner,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            throw new QueryValidationException("A SQL statement is required.");
        }

        return await runner.ExecuteAsync(request.Sql, [], cancellationToken).ConfigureAwait(false);
    }

    private static async Task<QueryResultSet> ExecuteModelAsync(
        QueryExecuteRequest request,
        ISchemaProjectionRepository projections,
        IQueryModelCompiler compiler,
        IQueryRunner runner,
        CancellationToken cancellationToken
    )
    {
        if (request.SourceId is null || request.Model is null)
        {
            throw new QueryValidationException("A source and a query model are required.");
        }

        IReadOnlyList<SchemaProjection> entities = await projections
            .ListBySourceAsync(request.SourceId.Value, cancellationToken)
            .ConfigureAwait(false);
        ProjectionCatalog catalog = ProjectionCatalogFactory.Create(entities);
        GeneratedSql generated = compiler.Compile(request.Model, catalog);

        return await runner
            .ExecuteAsync(generated.CommandText, generated.Parameters, cancellationToken)
            .ConfigureAwait(false);
    }

    private static QueryExecuteResponse Map(QueryResultSet result)
    {
        List<QueryColumnDto> columns = new List<QueryColumnDto>(result.Columns.Count);
        foreach (QueryResultColumn column in result.Columns)
        {
            columns.Add(new QueryColumnDto { Name = column.Name, DataType = column.DataType });
        }

        return new QueryExecuteResponse
        {
            Columns = columns,
            Rows = result.Rows,
            Truncated = result.Truncated,
        };
    }
}
