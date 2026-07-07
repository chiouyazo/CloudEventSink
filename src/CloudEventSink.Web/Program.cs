using System.Globalization;
using CloudEventSink.Web.Api;
using CloudEventSink.Web.Auth;
using CloudEventSink.Web.Components;
using CloudEventSink.Web.Ingest;
using CloudEventSink.Web.Startup;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(
        (context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    );

    builder.AddCloudEventSinkWeb();

    WebApplication app = builder.Build();

    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.UseAntiforgery();

    app.UseSwaggerBehindAuthentication();

    app.MapStaticAssets();
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

    app.MapIngestEndpoints();
    app.MapSourceEndpoints();
    app.MapEventEndpoints();
    app.MapSchemaEndpoints();
    app.MapProjectionEndpoints();
    app.MapQueryEndpoints();
    app.MapApiTokenEndpoints();
    app.MapAuthEndpoints();

    await DatabaseInitializer.InitializeAsync(app.Services, CancellationToken.None);

    await app.RunAsync();
    return 0;
}
catch (Exception exception)
    when (exception.GetType().Name is not ("HostAbortedException" or "StopTheHostException"))
{
    Log.Fatal(exception, "CloudEventSink terminated unexpectedly during startup.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
