using CloudEventSink.Infrastructure.Persistence;
using CloudEventSink.Web.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CloudEventSink.Web.Startup;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        IServiceProvider scoped = scope.ServiceProvider;

        AppDbContext dbContext = scoped.GetRequiredService<AppDbContext>();
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        }

        await SeedAdminAsync(scoped, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedAdminAsync(
        IServiceProvider scoped,
        CancellationToken cancellationToken
    )
    {
        AdminOptions admin = scoped.GetRequiredService<IOptions<AdminOptions>>().Value;
        ILogger logger = scoped
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("CloudEventSink.Startup");

        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogInformation(
                "Admin seeding skipped: no administrator credentials configured."
            );
            return;
        }

        UserManager<IdentityUser> userManager = scoped.GetRequiredService<
            UserManager<IdentityUser>
        >();
        if (await userManager.Users.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        IdentityUser user = new IdentityUser
        {
            UserName = admin.Email,
            Email = admin.Email,
            EmailConfirmed = true,
        };

        IdentityResult result = await userManager
            .CreateAsync(user, admin.Password)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(
                $"Failed to create the initial administrator: {errors}"
            );
        }

        logger.LogInformation("Initial administrator {Email} created.", admin.Email);
    }
}
