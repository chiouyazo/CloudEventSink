using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Web.Contracts;
using CloudEventSink.Web.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace CloudEventSink.Web.Api;

public static class EventEndpoints
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints
            .MapGroup("/api")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Events");

        group.MapGet("/sources/{id:guid}/events", ListAsync);
        group.MapGet("/events/{id:guid}", GetAsync);

        return endpoints;
    }

    [EndpointSummary("Lists events for a source with optional type filter and pagination.")]
    [ProducesResponseType<PagedResponse<EventListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private static async Task<IResult> ListAsync(
        Guid id,
        ISourceRepository sources,
        IEventRepository events,
        CancellationToken cancellationToken,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize
    )
    {
        Source? source = await sources.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return Results.NotFound();
        }

        int normalizedPage = page < 1 ? 1 : page;
        int normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        PagedResult<EventRecord> result = await events
            .ListBySourceAsync(id, type, normalizedPage, normalizedPageSize, cancellationToken)
            .ConfigureAwait(false);

        List<EventListItemResponse> items = new List<EventListItemResponse>(result.Items.Count);
        foreach (EventRecord record in result.Items)
        {
            items.Add(EventMapper.ToListItem(record));
        }

        PagedResponse<EventListItemResponse> response = new PagedResponse<EventListItemResponse>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };

        return Results.Ok(response);
    }

    [EndpointSummary("Gets the full envelope and data of a single event.")]
    [ProducesResponseType<EventDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private static async Task<IResult> GetAsync(
        Guid id,
        IEventRepository events,
        CancellationToken cancellationToken
    )
    {
        EventRecord? record = await events
            .GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? Results.NotFound() : Results.Ok(EventMapper.ToDetail(record));
    }
}
