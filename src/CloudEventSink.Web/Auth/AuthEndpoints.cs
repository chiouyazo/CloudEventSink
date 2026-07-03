using Microsoft.AspNetCore.Identity;

namespace CloudEventSink.Web.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/auth/login", LoginAsync).DisableAntiforgery();

        endpoints.MapPost("/auth/logout", LogoutAsync).RequireAuthorization().DisableAntiforgery();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        SignInManager<IdentityUser> signInManager
    )
    {
        IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
        string email = form["email"].ToString();
        string password = form["password"].ToString();
        bool rememberMe = string.Equals(
            form["rememberMe"].ToString(),
            "true",
            StringComparison.OrdinalIgnoreCase
        );
        string returnUrl = form["returnUrl"].ToString();

        SignInResult result = await signInManager.PasswordSignInAsync(
            email,
            password,
            rememberMe,
            lockoutOnFailure: true
        );
        if (result.Succeeded)
        {
            string target = IsSafeReturnUrl(returnUrl) ? returnUrl : "/";
            return Results.LocalRedirect(target);
        }

        string reason = result.IsLockedOut ? "locked" : "invalid";
        return Results.LocalRedirect($"/login?error={reason}");
    }

    private static async Task<IResult> LogoutAsync(SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.LocalRedirect("/login");
    }

    private static bool IsSafeReturnUrl(string returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//", StringComparison.Ordinal);
    }
}
