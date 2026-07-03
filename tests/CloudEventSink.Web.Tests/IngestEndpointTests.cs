using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Enums;
using CloudEventSink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudEventSink.Web.Tests;

public sealed class IngestEndpointTests : IClassFixture<CloudEventSinkWebApplicationFactory>
{
    private const string ContentType = "application/cloudevents+json";

    private const string ExamplePayload = """
        {
          "specversion": "1.0",
          "id": "11111111-1111-1111-1111-111111111111",
          "type": "example.item.reported",
          "source": "example://node-a",
          "subject": "group-1",
          "time": "2025-01-01T10:00:00Z",
          "datacontenttype": "application/json",
          "data": {
            "deviceId": "node-a",
            "label": "Example Node",
            "items": [
              { "itemId": "a1", "title": "First", "version": "1.0.0", "channel": null },
              { "itemId": "b2", "title": "Second", "version": "2.3.1", "channel": "stable" }
            ]
          }
        }
        """;

    private readonly CloudEventSinkWebApplicationFactory factory;

    public IngestEndpointTests(CloudEventSinkWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Ingest_WithoutAuthentication_ReturnsUnauthorized()
    {
        (Guid _, string slug, string _) = await SeedSourceAsync(SourceAuthMode.Bearer);
        HttpClient client = this.factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            $"/ingest/{slug}",
            new StringContent(ExamplePayload, Encoding.UTF8, ContentType),
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_WithWrongBearer_ReturnsUnauthorized()
    {
        (Guid _, string slug, string _) = await SeedSourceAsync(SourceAuthMode.Bearer);
        HttpClient client = this.factory.CreateClient();

        HttpRequestMessage request = BuildRequest(slug);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "not-the-right-token"
        );

        HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_WithValidBearer_ReturnsAcceptedAndStoresExactlyOneEvent()
    {
        (Guid id, string slug, string token) = await SeedSourceAsync(SourceAuthMode.Bearer);
        HttpClient client = this.factory.CreateClient();

        HttpRequestMessage request = BuildRequest(slug);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, await CountEventsAsync(id));
    }

    [Fact]
    public async Task Ingest_WithValidHmacSignature_ReturnsAccepted()
    {
        (Guid id, string slug, string secret) = await SeedSourceAsync(SourceAuthMode.Hmac);
        HttpClient client = this.factory.CreateClient();

        HttpRequestMessage request = BuildRequest(slug);
        request.Headers.Add("X-Signature", ComputeSignature(secret, ExamplePayload));

        HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, await CountEventsAsync(id));
    }

    [Fact]
    public async Task Ingest_WithTamperedBodyAndOldSignature_ReturnsUnauthorized()
    {
        (Guid _, string slug, string secret) = await SeedSourceAsync(SourceAuthMode.Hmac);
        HttpClient client = this.factory.CreateClient();

        string signature = ComputeSignature(secret, ExamplePayload);
        string tampered = ExamplePayload.Replace("node-a", "node-z", StringComparison.Ordinal);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"/ingest/{slug}")
        {
            Content = new StringContent(tampered, Encoding.UTF8, ContentType),
        };
        request.Headers.Add("X-Signature", signature);

        HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_ToUnknownSlug_ReturnsNotFound()
    {
        HttpClient client = this.factory.CreateClient();

        HttpRequestMessage request = BuildRequest("does-not-exist");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "whatever");

        HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HttpRequestMessage BuildRequest(string slug)
    {
        return new HttpRequestMessage(HttpMethod.Post, $"/ingest/{slug}")
        {
            Content = new StringContent(ExamplePayload, Encoding.UTF8, ContentType),
        };
    }

    private static string ComputeSignature(string secret, string payload)
    {
        byte[] hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(payload)
        );
        return $"sha256={Convert.ToHexStringLower(hash)}";
    }

    private async Task<(Guid Id, string Slug, string Secret)> SeedSourceAsync(
        SourceAuthMode authMode
    )
    {
        await using AsyncServiceScope scope = this.factory.Services.CreateAsyncScope();
        ISourceSecretService secretService =
            scope.ServiceProvider.GetRequiredService<ISourceSecretService>();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        IssuedSecret secret = secretService.Issue(authMode);
        string slug = $"src-{Guid.NewGuid():N}";

        Source source = new Source
        {
            Id = Guid.NewGuid(),
            Name = slug,
            Slug = slug,
            AuthMode = authMode,
            SecretHash = secret.StoredValue,
            SecretLastFour = secret.LastFour,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.Sources.Add(source);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (source.Id, slug, secret.PlaintextSecret);
    }

    private async Task<int> CountEventsAsync(Guid sourceId)
    {
        await using AsyncServiceScope scope = this.factory.Services.CreateAsyncScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Events.CountAsync(
            record => record.SourceId == sourceId,
            CancellationToken.None
        );
    }
}
