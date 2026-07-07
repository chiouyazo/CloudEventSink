using System.Security.Claims;
using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Security;
using CloudEventSink.Web.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CloudEventSink.Web.Api;

public static class ApiTokenEndpoints
{
    public static IEndpointRouteBuilder MapApiTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints
            .MapGroup("/api/api-tokens")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithTags("ApiTokens");

        group.MapGet(string.Empty, ListAsync);
        group.MapPost(string.Empty, CreateAsync);
        group.MapDelete("/{id:guid}", RevokeAsync);

        return endpoints;
    }

    [EndpointSummary("Lists API tokens (never the plaintext).")]
    [ProducesResponseType<IReadOnlyList<ApiTokenResponse>>(StatusCodes.Status200OK)]
    private static async Task<IResult> ListAsync(
        IApiTokenRepository tokens,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ApiToken> entities = await tokens
            .ListAsync(cancellationToken)
            .ConfigureAwait(false);
        List<ApiTokenResponse> response = new List<ApiTokenResponse>(entities.Count);
        foreach (ApiToken token in entities)
        {
            response.Add(ToResponse(token));
        }

        return Results.Ok(response);
    }

    [EndpointSummary("Creates an API token and returns the plaintext exactly once.")]
    [ProducesResponseType<ApiTokenCreatedResponse>(StatusCodes.Status201Created)]
    private static async Task<IResult> CreateAsync(
        CreateApiTokenRequest request,
        HttpContext context,
        IApiTokenRepository tokens,
        IUnitOfWork unitOfWork,
        IClock clock,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        string plaintext = ApiTokenHasher.Generate();
        DateTimeOffset now = clock.UtcNow;
        ApiToken token = new ApiToken
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            TokenHash = ApiTokenHasher.Hash(plaintext),
            TokenLastFour = ApiTokenHasher.LastFour(plaintext),
            CreatedBy = CurrentUserId(context),
            ExpiresAtUtc = request.ExpiresAtUtc,
            CreatedAtUtc = now,
        };

        tokens.Add(token);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ApiTokenCreatedResponse created = new ApiTokenCreatedResponse
        {
            Token = ToResponse(token),
            PlaintextToken = plaintext,
        };
        return Results.Created($"/api/api-tokens/{token.Id}", created);
    }

    [EndpointSummary("Revokes an API token.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private static async Task<IResult> RevokeAsync(
        Guid id,
        IApiTokenRepository tokens,
        IUnitOfWork unitOfWork,
        IClock clock,
        CancellationToken cancellationToken
    )
    {
        ApiToken? token = await tokens.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return Results.NotFound();
        }

        token.RevokedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static string CurrentUserId(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.Identity?.Name
            ?? "unknown";
    }

    private static ApiTokenResponse ToResponse(ApiToken token)
    {
        return new ApiTokenResponse
        {
            Id = token.Id,
            Name = token.Name,
            TokenLastFour = token.TokenLastFour,
            ExpiresAtUtc = token.ExpiresAtUtc,
            RevokedAtUtc = token.RevokedAtUtc,
            CreatedAtUtc = token.CreatedAtUtc,
            LastUsedAtUtc = token.LastUsedAtUtc,
        };
    }
}
