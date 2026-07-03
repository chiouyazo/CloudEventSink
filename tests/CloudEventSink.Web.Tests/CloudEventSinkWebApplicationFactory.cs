using CloudEventSink.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CloudEventSink.Web.Tests;

public sealed class CloudEventSinkWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"ces-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                Dictionary<string, string?> overrides = new Dictionary<string, string?>(
                    StringComparer.Ordinal
                )
                {
                    ["ConnectionStrings:Postgres"] =
                        "Host=unused;Database=unused;Username=unused;Password=unused",
                    ["Admin:Email"] = string.Empty,
                    ["Admin:Password"] = string.Empty,
                };
                configuration.AddInMemoryCollection(overrides);
            }
        );

        builder.ConfigureTestServices(services =>
        {
            RemoveExistingDbContext(services);
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(this.databaseName)
            );
        });
    }

    private static void RemoveExistingDbContext(IServiceCollection services)
    {
        List<ServiceDescriptor> descriptors = services.Where(IsAppDbContextRegistration).ToList();

        foreach (ServiceDescriptor descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private static bool IsAppDbContextRegistration(ServiceDescriptor descriptor)
    {
        Type serviceType = descriptor.ServiceType;
        if (
            serviceType == typeof(DbContextOptions<AppDbContext>)
            || serviceType == typeof(AppDbContext)
        )
        {
            return true;
        }

        return serviceType.IsGenericType
            && serviceType.GenericTypeArguments.Contains(typeof(AppDbContext));
    }
}
