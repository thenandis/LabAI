using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Infrastructure.Persistence.Seeding;

/// <summary>
/// Creates and seeds the SQLite database on startup, for local dev and integration tests.
/// </summary>
/// <remarks>
/// For SQL Server it deliberately does NOTHING: in production the schema is deployed by
/// the scripts in /SQLServerDb through a pipeline. Apps should not alter production
/// schemas at startup (several instances would race, and the app login would need DDL rights).
/// </remarks>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // The DbContext is SCOPED, so we must create a scope at startup (there's no HTTP request yet).
        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (!options.InitializeOnStartup) return;

        if (options.Provider != DatabaseProvider.Sqlite)
        {
            logger.LogInformation("Skipping DB initialisation: SQL Server schema is managed by the SQLServerDb scripts.");
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Categories.AnyAsync(cancellationToken))
            return; // already seeded

        logger.LogInformation("Seeding SQLite database with Northwind sample data...");

        foreach (var c in NorthwindSeedData.Categories)
        {
            var entity = new Category(c.Name, c.Description);
            db.Categories.Add(entity);
            // Keep the original Northwind IDs, so products' foreign keys line up.
            db.Entry(entity).Property(e => e.Id).CurrentValue = c.Id;
        }

        foreach (var s in NorthwindSeedData.Suppliers)
        {
            var entity = new Supplier(s.CompanyName, s.ContactName, s.City, s.Country, s.Phone);
            db.Suppliers.Add(entity);
            db.Entry(entity).Property(e => e.Id).CurrentValue = s.Id;
        }

        foreach (var p in NorthwindSeedData.Products)
        {
            // Seed through the domain factory, so even seed data obeys the business rules.
            var entity = Product.Create(new ProductDetails(
                p.Name, p.SupplierId, p.CategoryId, p.QuantityPerUnit, p.UnitPrice,
                p.UnitsInStock, p.UnitsOnOrder, p.ReorderLevel, p.Discontinued));
            db.Products.Add(entity);
            db.Entry(entity).Property(e => e.Id).CurrentValue = p.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} products.", NorthwindSeedData.Products.Length);
    }
}
