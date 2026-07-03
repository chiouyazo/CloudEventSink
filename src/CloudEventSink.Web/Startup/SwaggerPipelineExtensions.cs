using Microsoft.AspNetCore.Authentication;

namespace CloudEventSink.Web.Startup;

public static class SwaggerPipelineExtensions
{
    private const string SwaggerPathPrefix = "/swagger";

    public static WebApplication UseSwaggerBehindAuthentication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Use(
            async (context, next) =>
            {
                if (
                    context.Request.Path.StartsWithSegments(
                        SwaggerPathPrefix,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && context.User.Identity?.IsAuthenticated != true
                )
                {
                    await context.ChallengeAsync().ConfigureAwait(false);
                    return;
                }

                await next(context).ConfigureAwait(false);
            }
        );

        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}
