using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Web.Contracts;
using CloudEventSink.Web.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace CloudEventSink.Web.Api;

public static class SchemaEndpoints
{
    public static IEndpointRouteBuilder MapSchemaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints
            .MapGroup("/api/sources/{id:guid}/schemas")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Schemas");

        group.MapGet(string.Empty, ListAsync);
        group.MapGet("/{eventType}", GetAsync);

        return endpoints;
    }

    [EndpointSummary("Lists the inferred schemas of a source.")]
    [ProducesResponseType<IReadOnlyList<SchemaSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private static async Task<IResult> ListAsync(
        Guid id,
        ISourceRepository sources,
        ISchemaRepository schemas,
        CancellationToken cancellationToken
    )
    {
        Source? source = await sources.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return Results.NotFound();
        }

        IReadOnlyList<InferredSchema> entities = await schemas
            .ListBySourceAsync(id, cancellationToken)
            .ConfigureAwait(false);
        List<SchemaSummaryResponse> response = new List<SchemaSummaryResponse>(entities.Count);
        foreach (InferredSchema schema in entities)
        {
            response.Add(SchemaMapper.ToSummary(schema));
        }

        return Results.Ok(response);
    }

    [EndpointSummary("Gets the inferred field tree of a source for one event type.")]
    [ProducesResponseType<SchemaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private static async Task<IResult> GetAsync(
        Guid id,
        string eventType,
        ISchemaRepository schemas,
        CancellationToken cancellationToken
    )
    {
        InferredSchema? schema = await schemas
            .GetAsync(id, eventType, cancellationToken)
            .ConfigureAwait(false);
        return schema is null ? Results.NotFound() : Results.Ok(SchemaMapper.ToResponse(schema));
    }
}
