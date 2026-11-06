using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SharpAPI.Application.Abstractions;
using SharpAPI.Infrastructure.Persistence;
using SharpAPI.Infrastructure.Persistence.Interceptors;
using SharpAPI.Infrastructure.Persistence.Queries;
using SharpAPI.Infrastructure.Persistence.Repositories;

namespace SharpAPI.Infrastructure;

/// <summary>
/// Registers everything Infrastructure provides. This is where the Application's
/// interfaces get bound to their EF Core implementations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- Options: bind + validate at startup (fail fast on bad config) ----
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(configuration.GetConnectionString(o.ConnectionStringName)),
                "A connection string named by Database:ConnectionStringName is required.")
            .Validate(o => o.CommandTimeoutSeconds is > 0 and <= 600, "CommandTimeoutSeconds must be 1-600.")
            .ValidateOnStart();

        // TimeProvider.System in production; tests can swap in a FakeTimeProvider.
        services.TryAddSingleton(TimeProvider.System);

        // Interceptors are singletons (stateless), as DbContext pooling requires.
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<SqliteRowVersionInterceptor>();

        // ---- DbContext with POOLING ----
        // AddDbContextPool reuses DbContext instances across requests (they are reset between uses).
        services.AddDbContextPool<AppDbContext>((sp, options) =>
        {
            var db = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = configuration.GetConnectionString(db.ConnectionStringName)!;

            switch (db.Provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString, sql =>
                    {
                        // CONNECTION RESILIENCY: retry transient failures (Azure SQL failover,
                        // throttling, deadlock victim) with exponential back-off.
                        sql.EnableRetryOnFailure(db.MaxRetryCount, TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                        sql.CommandTimeout(db.CommandTimeoutSeconds);
                    });
                    break;

                case DatabaseProvider.Sqlite:
                    options.UseSqlite(connectionString, sqlite => sqlite.CommandTimeout(db.CommandTimeoutSeconds));
                    options.AddInterceptors(sp.GetRequiredService<SqliteRowVersionInterceptor>());
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported database provider '{db.Provider}'.");
            }

            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());

            if (db.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging().EnableDetailedErrors();
        }, poolSize: 256);

        // ---- Ports → Adapters ----
        // Scoped = one per HTTP request, sharing the request's DbContext. That's what makes
        // repository + unit of work operate on the SAME change tracker.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<ILookupQueries, LookupQueries>();

        // ---- Health check ----
        // /health/ready runs a cheap query. Kubernetes or a load balancer stops sending
        // traffic to an instance that can't reach its database.
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

        return services;
    }
}
