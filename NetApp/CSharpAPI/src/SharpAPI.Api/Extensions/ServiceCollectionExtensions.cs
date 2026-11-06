using System.Diagnostics;
using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.ResponseCompression;
using SharpAPI.Api.Caching;
using SharpAPI.Api.ErrorHandling;
using SharpAPI.Api.Serialization;

namespace SharpAPI.Api.Extensions;

/// <summary>
/// Registers the HTTP-layer services: everything that makes the API production-grade
/// but has nothing to do with business logic.
/// </summary>
internal static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ---------------------------------------------------------------------
        // 1. ERROR HANDLING: RFC 9457 ProblemDetails
        //    Every error (400/404/409/500) has the same JSON shape:
        //    { type, title, status, detail, instance, traceId, ... }
        //    The Angular interceptor handles errors generically because of this.
        // ---------------------------------------------------------------------
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
                // traceId lets support staff match the error a user reports to the server logs
                ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // ---------------------------------------------------------------------
        // 2. JSON: System.Text.Json SOURCE GENERATION
        //    Serialisation code is generated at COMPILE time, with no runtime reflection:
        //    faster, fewer allocations, and trimming/AOT friendly. Types not in the
        //    context fall back to the default reflection-based resolver.
        // ---------------------------------------------------------------------
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

        // ---------------------------------------------------------------------
        // 3. CORS: only needed in local dev (Angular on :4200, API on :5080).
        //    In production nginx serves the UI and proxies /api on the SAME origin, so no CORS.
        // ---------------------------------------------------------------------
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader()
            .SetPreflightMaxAge(TimeSpan.FromHours(1)))); // the browser caches the OPTIONS preflight

        // ---------------------------------------------------------------------
        // 4. OUTPUT CACHING: server-side response cache
        //    The first GET runs the SQL; identical GETs within the window are served from
        //    memory in microseconds. Writes EVICT by tag, so users never see stale data
        //    after they save.
        //    Scale-out note: this store is in-memory per instance. With several instances,
        //    swap in Redis (AddStackExchangeRedisOutputCache) so evictions reach all of them.
        // ---------------------------------------------------------------------
        services.AddOutputCache(options =>
        {
            options.AddPolicy(CachePolicies.Products, policy => policy
                .Expire(TimeSpan.FromSeconds(60))
                .Tag(CachePolicies.ProductsTag));

            options.AddPolicy(CachePolicies.Lookups, policy => policy
                .Expire(TimeSpan.FromMinutes(5))
                .Tag(CachePolicies.LookupsTag));
        });

        // ---------------------------------------------------------------------
        // 5. RESPONSE COMPRESSION: Brotli first, then gzip.
        //    A page of 100 products is ~30 KB of JSON → ~4 KB compressed.
        // ---------------------------------------------------------------------
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

        // ---------------------------------------------------------------------
        // 6. RATE LIMITING: fixed window per client IP → 429 Too Many Requests.
        //    Protects the DB from abusive clients and runaway scripts.
        //    Behind a proxy, set ASPNETCORE_FORWARDEDHEADERS_ENABLED=true so RemoteIpAddress
        //    is the real client IP and not the proxy's (docker-compose does this).
        // ---------------------------------------------------------------------
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 100);
        var windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 10);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0
                    }));
            options.OnRejected = (ctx, _) =>
            {
                ctx.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString();
                return ValueTask.CompletedTask;
            };
        });

        // ---------------------------------------------------------------------
        // 7. REQUEST TIMEOUTS: no request may hold a thread/DB connection forever.
        //    Cancels the request's CancellationToken after 30s, which EF Core passes on to SQL.
        // ---------------------------------------------------------------------
        services.AddRequestTimeouts(options =>
            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(30),
                TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
            });

        // ---------------------------------------------------------------------
        // 8. OPENAPI: built into .NET 9+ (no Swashbuckle). Document at /openapi/v1.json,
        //    interactive UI at /scalar (Development only).
        // ---------------------------------------------------------------------
        services.AddOpenApi();

        return services;
    }
}
