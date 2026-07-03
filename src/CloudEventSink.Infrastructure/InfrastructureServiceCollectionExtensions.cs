using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Ingestion;
using CloudEventSink.Core.Schema;
using CloudEventSink.Infrastructure.Persistence;
using CloudEventSink.Infrastructure.Persistence.Repositories;
using CloudEventSink.Infrastructure.Security;
using CloudEventSink.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudEventSink.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<ISourceRepository, SourceRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISchemaRepository, SchemaRepository>();
        services.AddScoped<IEventIngestionService, EventIngestionService>();

        services.AddSingleton<ISchemaInferenceService, SchemaInferenceService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISourceSecretService, SourceSecretService>();

        return services;
    }
}
