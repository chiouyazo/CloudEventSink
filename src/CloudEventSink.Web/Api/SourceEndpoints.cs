using System.Text.RegularExpressions;
using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Web.Contracts;
using CloudEventSink.Web.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace CloudEventSink.Web.Api;

public static class SourceEndpoints
{
    private static readonly Regex SlugPattern = new Regex(
        "^[a-z0-9-]{1,120}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200)
    );

    public static IEndpointRouteBuilder MapSourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints
            .MapGroup("/api/sources")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("Sources");

        group.MapGet(string.Empty, ListAsync);
        group.MapPost(string.Empty, CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
        group.MapPost("/{id:guid}/rotate-secret", RotateSecretAsync);

        return endpoints;
    }

    [EndpointSummary("Lists all configured sources.")]
    [ProducesResponseType<IReadOnlyList<SourceResponse>>(StatusCodes.Status200OK)]
    private static async Task<IResult> ListAsync(
        ISourceRepository sources,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Source> entities = await sources
            .ListAsync(cancellationToken)
            .ConfigureAwait(false);
        List<SourceResponse> response = new List<SourceResponse>(entities.Count);
        foreach (Source source in entities)
        {
            response.Add(SourceMapper.ToResponse(source));
        }

        return Results.Ok(response);
    }

    [EndpointSummary("Creates a source and returns the plaintext secret exactly once.")]
    [ProducesResponseType<SourceCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    private static async Task<IResult> CreateAsync(
        CreateSourceRequest request,
        ISourceRepository sources,
        ISourceSecretService secretService,
        IUnitOfWork unitOfWork,
        IClock clock,
        CancellationToken cancellationToken
    )
    {
        if (!SlugPattern.IsMatch(request.Slug) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        if (
            await sources
                .SlugExistsAsync(request.Slug, null, cancellationToken)
                .ConfigureAwait(false)
            || await sources
                .NameExistsAsync(request.Name, null, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Results.Conflict();
        }

        IssuedSecret secret = secretService.Issue(request.AuthMode);
        DateTimeOffset now = clock.UtcNow;

        Source source = new Source
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            AuthMode = request.AuthMode,
            SecretHash = secret.StoredValue,
            SecretLastFour = secret.LastFour,
            IpAllowlist = NormalizeAllowlist(request.IpAllowlist),
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        sources.Add(source);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        SourceCreatedResponse created = new SourceCreatedResponse
        {
            Source = SourceMapper.ToResponse(source),
            PlaintextSecret = secret.PlaintextSecret,
        };

        return Results.Created($"/api/sources/{source.Id}", created);
    }

    [EndpointSummary("Gets a single source by identifier.")]
    [ProducesResponseType<SourceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private static async Task<IResult> GetAsync(
        Guid id,
        ISourceRepository sources,
        CancellationToken cancellationToken
    )
    {
        Source? source = await sources.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return source is null ? Results.NotFound() : Results.Ok(SourceMapper.ToResponse(source));
    }

    [EndpointSummary("Updates the metadata of a source.")]
    [ProducesResponseType<SourceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateSourceRequest request,
        ISourceRepository sources,
        IUnitOfWork unitOfWork,
        IClock clock,
        CancellationToken cancellationToken
    )
    {
        if (!SlugPattern.IsMatch(request.Slug) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        Source? source = await sources.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return Results.NotFound();
        }

        if (
            await sources.SlugExistsAsync(request.Slug, id, cancellationToken).ConfigureAwait(false)
            || await sources
                .NameExistsAsync(request.Name, id, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Results.Conflict();
        }

        source.Name = request.Name;
        source.Slug = request.Slug;
        source.IpAllowlist = NormalizeAllowlist(request.IpAllowlist);
        source.IsActive = request.IsActive;
        source.UpdatedAtUtc = clock.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Results.Ok(SourceMapper.ToResponse(source));
    }

    [EndpointSummary("Deletes a source and all of its events and schemas.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private static async Task<IResult> DeleteAsync(
        Guid id,
        ISourceRepository sources,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken
    )
    {
        Source? source = await sources.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return Results.NotFound();
        }

        sources.Remove(source);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    [EndpointSummary("Rotates the secret of a source and returns the new plaintext secret once.")]
    [ProducesResponseType<SecretRotatedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private static async Task<IResult> RotateSecretAsync(
        Guid id,
        ISourceRepository sources,
        ISourceSecretService secretService,
        IUnitOfWork unitOfWork,
        IClock clock,
        CancellationToken cancellationToken
    )
    {
        Source? source = await sources.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return Results.NotFound();
        }

        IssuedSecret secret = secretService.Issue(source.AuthMode);
        source.SecretHash = secret.StoredValue;
        source.SecretLastFour = secret.LastFour;
        source.UpdatedAtUtc = clock.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        SecretRotatedResponse response = new SecretRotatedResponse
        {
            SourceId = source.Id,
            SecretLastFour = secret.LastFour,
            PlaintextSecret = secret.PlaintextSecret,
        };

        return Results.Ok(response);
    }

    private static string? NormalizeAllowlist(string? allowlist)
    {
        return string.IsNullOrWhiteSpace(allowlist) ? null : allowlist.Trim();
    }
}
