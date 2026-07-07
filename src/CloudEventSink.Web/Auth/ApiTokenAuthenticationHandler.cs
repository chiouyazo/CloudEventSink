using System.Security.Claims;
using System.Text.Encodings.Web;
using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CloudEventSink.Web.Auth;

public sealed class ApiTokenAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiToken";

    private readonly IServiceScopeFactory scopeFactory;

    public ApiTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceScopeFactory scopeFactory
    )
        : base(options, logger, encoder)
    {
        this.scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string authorization = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string token = authorization[prefix.Length..].Trim();
        if (token.Length == 0)
        {
            return AuthenticateResult.NoResult();
        }

        string hash = ApiTokenHasher.Hash(token);
        await using AsyncServiceScope scope = this.scopeFactory.CreateAsyncScope();
        IApiTokenRepository repository =
            scope.ServiceProvider.GetRequiredService<IApiTokenRepository>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();
        DateTimeOffset now = clock.UtcNow;
        ApiToken? found = await repository
            .GetActiveByHashAsync(hash, now, Context.RequestAborted)
            .ConfigureAwait(false);

        if (found is null)
        {
            return AuthenticateResult.Fail("Invalid API token.");
        }

        await repository
            .TouchLastUsedAsync(found.Id, now, Context.RequestAborted)
            .ConfigureAwait(false);

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, found.CreatedBy),
            new Claim("token_id", found.Id.ToString()),
            new Claim("auth_type", "apitoken"),
        ];
        ClaimsIdentity identity = new ClaimsIdentity(claims, Scheme.Name);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
