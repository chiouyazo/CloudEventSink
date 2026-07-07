using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Projection;
using CloudEventSink.Web.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CloudEventSink.Web.Api;

public static class ProjectionEndpoints
{
    public static IEndpointRouteBuilder MapProjectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapGet("/api/sources/{id:guid}/fields", FieldsAsync)
            .RequireAuthorization(Auth.AuthorizationPolicies.QueryAccess)
            .DisableAntiforgery()
            .WithTags("Projections");

        endpoints
            .MapPost("/api/sources/{id:guid}/projections/regenerate", RegenerateAsync)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Projections");

        return endpoints;
    }

    [EndpointSummary("Lists the projected views and columns available for querying a source.")]
    [ProducesResponseType<FieldCatalogResponse>(StatusCodes.Status200OK)]
    private static async Task<IResult> FieldsAsync(
        Guid id,
        ISchemaProjectionRepository projections,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<SchemaProjection> entities = await projections
            .ListBySourceAsync(id, cancellationToken)
            .ConfigureAwait(false);

        List<FieldViewDto> views = new List<FieldViewDto>();
        foreach (SchemaProjection projection in entities)
        {
            views.Add(
                new FieldViewDto
                {
                    Name = projection.MainViewName,
                    IsChild = false,
                    EventType = projection.EventType,
                    Columns = MapColumns(
                        ProjectionSerializer.DeserializeColumns(projection.ColumnsJson)
                    ),
                }
            );

            foreach (
                ProjectedView child in ProjectionSerializer.DeserializeViews(
                    projection.ChildViewsJson
                )
            )
            {
                views.Add(
                    new FieldViewDto
                    {
                        Name = child.Name,
                        IsChild = true,
                        EventType = projection.EventType,
                        Columns = MapColumns(child.Columns),
                    }
                );
            }
        }

        return Results.Ok(new FieldCatalogResponse { SourceId = id, Views = views });
    }

    [EndpointSummary("Regenerates the projected views for a source from its inferred schema.")]
    [ProducesResponseType<RegenerateResponse>(StatusCodes.Status200OK)]
    private static async Task<IResult> RegenerateAsync(
        Guid id,
        IProjectionService projectionService,
        CancellationToken cancellationToken
    )
    {
        int generated = await projectionService
            .RegenerateAsync(id, cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(new RegenerateResponse { Generated = generated });
    }

    private static List<FieldColumnDto> MapColumns(IReadOnlyList<ProjectedColumn> columns)
    {
        List<FieldColumnDto> result = new List<FieldColumnDto>(columns.Count);
        foreach (ProjectedColumn column in columns)
        {
            result.Add(new FieldColumnDto { Name = column.Name, SqlType = column.SqlType });
        }

        return result;
    }
}
