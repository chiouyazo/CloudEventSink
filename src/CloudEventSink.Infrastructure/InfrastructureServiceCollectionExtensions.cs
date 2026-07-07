using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Ingestion;
using CloudEventSink.Core.Projection;
using CloudEventSink.Core.Query;
using CloudEventSink.Core.Schema;
using CloudEventSink.Infrastructure.Persistence;
using CloudEventSink.Infrastructure.Persistence.Repositories;
using CloudEventSink.Infrastructure.Projection;
using CloudEventSink.Infrastructure.Query;
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
        services.AddScoped<ISchemaProjectionRepository, SchemaProjectionRepository>();
        services.AddScoped<ISavedQueryRepository, SavedQueryRepository>();
        services.AddScoped<IQueryFolderRepository, QueryFolderRepository>();
        services.AddScoped<IApiTokenRepository, ApiTokenRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IEventIngestionService, EventIngestionService>();
        services.AddScoped<IProjectionService, ProjectionService>();

        services.AddSingleton<ISchemaInferenceService, SchemaInferenceService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISourceSecretService, SourceSecretService>();
        services.AddSingleton<IProjectionGenerator, ProjectionGenerator>();
        services.AddSingleton<IQueryModelCompiler, QueryModelCompiler>();
        services.AddSingleton<IQueryRunner>(_ => new QueryRunner(connectionString));

        return services;
    }
}
