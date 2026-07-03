using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CloudEventSink.Web.Auth;

public sealed class IdentityRevalidatingAuthenticationStateProvider
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IdentityOptions options;

    public IdentityRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<IdentityOptions> optionsAccessor
    )
        : base(loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        this.scopeFactory = scopeFactory;
        this.options = optionsAccessor.Value;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(authenticationState);

        await using AsyncServiceScope scope = this.scopeFactory.CreateAsyncScope();
        UserManager<IdentityUser> userManager = scope.ServiceProvider.GetRequiredService<
            UserManager<IdentityUser>
        >();
        return await ValidateSecurityStampAsync(userManager, authenticationState.User)
            .ConfigureAwait(false);
    }

    private async Task<bool> ValidateSecurityStampAsync(
        UserManager<IdentityUser> userManager,
        ClaimsPrincipal principal
    )
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        if (!userManager.SupportsUserSecurityStamp)
        {
            return true;
        }

        string? principalStamp = principal.FindFirstValue(
            this.options.ClaimsIdentity.SecurityStampClaimType
        );
        string userStamp = await userManager.GetSecurityStampAsync(user).ConfigureAwait(false);
        return string.Equals(principalStamp, userStamp, StringComparison.Ordinal);
    }
}
