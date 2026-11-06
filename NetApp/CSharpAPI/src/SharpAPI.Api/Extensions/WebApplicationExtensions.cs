using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using SharpAPI.Api.Endpoints;
using SharpAPI.Api.Middleware;

namespace SharpAPI.Api.Extensions;

/// <summary>
/// The middleware pipeline and route mapping.
/// </summary>
/// <remarks>
/// <para><b>MIDDLEWARE ORDER MATTERS</b> (a common interview question). Each middleware wraps
/// the ones registered after it, like layers of an onion: a request travels down the list,
/// and the response travels back up.</para>
/// </remarks>
internal static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // 1. Outermost: catches exceptions from EVERYTHING below and returns ProblemDetails.
        app.UseExceptionHandler();

        // 2. Turns bare error status codes (e.g. a 404 for an unknown route, or a 400 for
        //    malformed JSON) into ProblemDetails bodies too.
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            // HSTS tells browsers "always use HTTPS for this host". TLS itself usually
            // terminates at the ingress/load balancer in front of the container.
            app.UseHsts();
        }

        // 3. Security headers on every response.
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // 4. Compress responses (must run before anything that writes the body).
        app.UseResponseCompression();

        // 5. CORS must come before caching and endpoints, so preflight requests are answered.
        app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);

        // 6. Reject abusive clients early, before they cost any DB work.
        app.UseRateLimiter();

        // 7. Per-request timeout.
        app.UseRequestTimeouts();

        // 8. Serve cached responses (short-circuits the pipeline on a cache hit).
        app.UseOutputCache();

        return app;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapProductEndpoints();
        app.MapLookupEndpoints();

        // LIVENESS: "is the process alive?" Runs no checks, so it's always cheap.
        //   Kubernetes RESTARTS the container if this fails.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .DisableRateLimiting();

        // READINESS: "can I serve traffic?" Checks the database.
        //   Kubernetes stops ROUTING traffic here (no restart) while this fails.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") })
            .DisableRateLimiting();

        if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
        {
            app.MapOpenApi();                // /openapi/v1.json
            app.MapScalarApiReference();     // /scalar: interactive docs + "try it" client
            app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
        }

        return app;
    }
}
