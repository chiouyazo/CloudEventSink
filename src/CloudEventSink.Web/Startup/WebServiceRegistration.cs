using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CloudEventSink.Infrastructure;
using CloudEventSink.Infrastructure.Persistence;
using CloudEventSink.Web.Auth;
using CloudEventSink.Web.Configuration;
using CloudEventSink.Web.Ingest;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;

namespace CloudEventSink.Web.Startup;

public static class WebServiceRegistration
{
    public static WebApplicationBuilder AddCloudEventSinkWeb(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<AdminOptions>(
            builder.Configuration.GetSection(AdminOptions.SectionName)
        );
        builder.Services.Configure<IngestOptions>(
            builder.Configuration.GetSection(IngestOptions.SectionName)
        );
        builder.Services.Configure<RateLimitOptions>(
            builder.Configuration.GetSection(RateLimitOptions.SectionName)
        );

        ConfigureForwardedHeaders(builder);

        string connectionString =
            builder.Configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "The connection string 'Postgres' is not configured."
            );
        builder.Services.AddInfrastructure(connectionString);

        builder
            .Services.AddDataProtection()
            .SetApplicationName("CloudEventSink")
            .PersistKeysToDbContext<AppDbContext>();

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
        );

        ConfigureIdentity(builder);
        ConfigureRateLimiting(builder);

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddMudServices();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        return builder;
    }

    private static void ConfigureForwardedHeaders(WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    private static void ConfigureIdentity(WebApplicationBuilder builder)
    {
        builder
            .Services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        builder
            .Services.AddIdentityCore<IdentityUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/auth/logout";
            options.AccessDeniedPath = "/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<
            AuthenticationStateProvider,
            IdentityRevalidatingAuthenticationStateProvider
        >();
    }

    private static void ConfigureRateLimiting(WebApplicationBuilder builder)
    {
        int perMinute = builder.Configuration.GetValue("RateLimit:IngestPerMinute", 100);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(IngestEndpoints.RateLimitPolicy, CreateIngestLimiter(perMinute));
        });
    }

    private static Func<HttpContext, RateLimitPartition<string>> CreateIngestLimiter(int perMinute)
    {
        return context =>
        {
            string partitionKey = context.Request.RouteValues.TryGetValue("slug", out object? value)
                ? value?.ToString() ?? "unknown"
                : "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = perMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }
            );
        };
    }
}
