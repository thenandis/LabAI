using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SharpAPI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Emulates SQL Server's ROWVERSION on SQLite by assigning a fresh random token to every
/// inserted or updated row that has a "RowVersion" property.
/// </summary>
/// <remarks>
/// Registered ONLY when Provider = Sqlite (see DependencyInjection.cs). Optimistic
/// concurrency then behaves identically in local dev, integration tests and
/// production, and the services never need to know which database they run on.
/// </remarks>
internal sealed class SqliteRowVersionInterceptor : SaveChangesInterceptor
{
    private const string RowVersionProperty = "RowVersion";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Bump(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Bump(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Bump(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            if (entry.Metadata.FindProperty(RowVersionProperty) is null) continue;

            // OriginalValue (the client's version) stays as-is for the WHERE clause;
            // CurrentValue becomes the new version written to the row.
            entry.Property(RowVersionProperty).CurrentValue = Guid.NewGuid().ToByteArray();
        }
    }
}
