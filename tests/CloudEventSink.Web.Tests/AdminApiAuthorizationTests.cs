using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CloudEventSink.Web.Tests;

public sealed class AdminApiAuthorizationTests : IClassFixture<CloudEventSinkWebApplicationFactory>
{
    private readonly CloudEventSinkWebApplicationFactory factory;

    public AdminApiAuthorizationTests(CloudEventSinkWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AdminApi_WithoutLogin_IsNotAccessible()
    {
        WebApplicationFactoryClientOptions options = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        };
        HttpClient client = this.factory.CreateClient(options);

        HttpResponseMessage response = await client.GetAsync(
            "/api/sources",
            CancellationToken.None
        );

        Assert.True(
            response.StatusCode
                is HttpStatusCode.Unauthorized
                    or HttpStatusCode.Found
                    or HttpStatusCode.Redirect,
            $"Expected 401 or a redirect to login, but received {(int)response.StatusCode}."
        );
    }
}
