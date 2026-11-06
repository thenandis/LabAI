using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace SharpAPI.IntegrationTests;

/// <summary>
/// Boots the real API in memory (no network port) against a private in-memory SQLite
/// database, seeded with the Northwind data on startup.
/// </summary>
/// <remarks>
/// An in-memory SQLite database lives only while at least one connection is open, so
/// we hold a "keep-alive" connection for the lifetime of the factory. The GUID gives every
/// test class its own isolated database.
/// For maximum fidelity, swap this for Testcontainers (a real SQL Server in Docker).
/// </remarks>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _keepAlive =
        new($"Data Source=file:catalog-tests-{Guid.NewGuid():N}?mode=memory&cache=shared");

    public CatalogApiFactory() => _keepAlive.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // In-memory config is added LAST, so it overrides appsettings.json.
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Catalog"] = _keepAlive.ConnectionString,
            ["Database:Provider"] = "Sqlite",
            ["Database:InitializeOnStartup"] = "true",
            ["Logging:Console:FormatterName"] = "simple"
        }));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _keepAlive.DisposeAsync();
    }
}
