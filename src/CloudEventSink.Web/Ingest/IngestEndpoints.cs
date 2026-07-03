using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Enums;
using CloudEventSink.Core.Ingestion;
using CloudEventSink.Web.Configuration;
using CloudEventSink.Web.Security;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CloudEventSink.Web.Ingest;

public static class IngestEndpoints
{
    public const string RateLimitPolicy = "ingest";

    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost("/ingest/{slug}", HandleAsync)
            .RequireRateLimiting(RateLimitPolicy)
            .DisableAntiforgery()
            .WithName("IngestCloudEvent");

        return endpoints;
    }

    [EndpointSummary("Accepts a CloudEvent from a configured source.")]
    [Tags("Ingest")]
    [Consumes("application/cloudevents+json", "application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    private static async Task<IResult> HandleAsync(
        HttpContext context,
        string slug,
        ISourceRepository sources,
        ISourceSecretService secretService,
        IEventIngestionService ingestion,
        IOptions<IngestOptions> ingestOptions,
        ILoggerFactory loggerFactory
    )
    {
        ILogger logger = loggerFactory.CreateLogger("CloudEventSink.Ingest");
        long maxBytes = ingestOptions.Value.MaxBodyBytes;

        if (context.Request.ContentLength is long declaredLength && declaredLength > maxBytes)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        ApplyBodySizeLimit(context, maxBytes);

        byte[]? body = await ReadBodyAsync(context, context.RequestAborted).ConfigureAwait(false);
        if (body is null)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        Source? source = await sources
            .GetBySlugAsync(slug, context.RequestAborted)
            .ConfigureAwait(false);
        if (source is null)
        {
            return Results.NotFound();
        }

        if (!source.IsActive)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!IpAllowlistEvaluator.IsAllowed(source.IpAllowlist, context.Connection.RemoteIpAddress))
        {
            logger.LogWarning(
                "Ingest rejected for source {Slug}: remote address not in allowlist.",
                source.Slug
            );
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!IsAuthenticated(context, secretService, source, body))
        {
            logger.LogWarning(
                "Ingest rejected for source {Slug}: authentication failed.",
                source.Slug
            );
            return Results.Unauthorized();
        }

        if (
            !CloudEventReader.TryRead(
                body,
                context.Request.ContentType,
                out IncomingEvent incomingEvent
            )
        )
        {
            return Results.BadRequest();
        }

        await ingestion
            .IngestAsync(source.Id, incomingEvent, context.RequestAborted)
            .ConfigureAwait(false);
        return Results.StatusCode(StatusCodes.Status202Accepted);
    }

    private static void ApplyBodySizeLimit(HttpContext context, long maxBytes)
    {
        IHttpMaxRequestBodySizeFeature? feature =
            context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is not null && !feature.IsReadOnly)
        {
            feature.MaxRequestBodySize = maxBytes;
        }
    }

    private static async Task<byte[]?> ReadBodyAsync(
        HttpContext context,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using MemoryStream buffer = new MemoryStream();
            await context.Request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            return buffer.ToArray();
        }
        catch (BadHttpRequestException)
        {
            return null;
        }
    }

    private static bool IsAuthenticated(
        HttpContext context,
        ISourceSecretService secretService,
        Source source,
        byte[] body
    )
    {
        return source.AuthMode switch
        {
            SourceAuthMode.Bearer => VerifyBearer(context, secretService, source.SecretHash),
            SourceAuthMode.Hmac => VerifyHmac(context, secretService, source.SecretHash, body),
            _ => false,
        };
    }

    private static bool VerifyBearer(
        HttpContext context,
        ISourceSecretService secretService,
        string storedValue
    )
    {
        string authorization = context.Request.Headers.Authorization.ToString();
        const string scheme = "Bearer ";
        if (!authorization.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string token = authorization[scheme.Length..].Trim();
        return secretService.VerifyBearer(token, storedValue);
    }

    private static bool VerifyHmac(
        HttpContext context,
        ISourceSecretService secretService,
        string storedValue,
        byte[] body
    )
    {
        string signature = context.Request.Headers["X-Signature"].ToString();
        return secretService.VerifyHmac(storedValue, body, signature);
    }
}
