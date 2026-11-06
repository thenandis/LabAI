using Microsoft.EntityFrameworkCore;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Infrastructure.Persistence;

/// <summary>
/// EF Core's gateway to the database. One instance = one Unit of Work + identity map + change tracker.
/// </summary>
/// <remarks>
/// <para>
/// <b>DbContext pooling</b> (see DependencyInjection.cs): rather than constructing a new
/// context per request, ASP.NET rents a reset instance from a pool. That saves
/// allocations and setup on every request, a measurable throughput gain under load. The
/// requirement: the constructor may only take <see cref="DbContextOptions{TContext}"/>
/// and the context must hold no per-request state.
/// </para>
/// <para>
/// <c>internal</c>: nothing outside Infrastructure touches EF Core. The Application layer
/// sees only IProductRepository / IProductQueries / IUnitOfWork.
/// </para>
/// </remarks>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Each entity's mapping lives in its own IEntityTypeConfiguration<T> class
        // (Single Responsibility; avoids a 1,000-line OnModelCreating). This line finds them all.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ---- Provider differences ----
        // The mappings target SQL Server (production). SQLite, used for local dev and
        // tests, lacks two features, so we adapt here and keep all the other code
        // provider-agnostic:
        if (!Database.IsSqlServer())
        {
            modelBuilder.Entity<Product>(b =>
            {
                // 1. SQLite has no auto-updating ROWVERSION column. Keep it as a plain
                //    concurrency token; SqliteRowVersionInterceptor assigns a new value on each save.
                b.Property(p => p.RowVersion).ValueGeneratedNever().IsConcurrencyToken();

                // 2. SQLite can't ORDER BY or compare decimals in SQL. Store them as REAL.
                b.Property(p => p.UnitPrice).HasConversion<double>();
            });
        }
    }
}
