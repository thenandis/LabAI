using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharpAPI.Domain.Common;

namespace SharpAPI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps CreatedAtUtc / ModifiedAtUtc on every <see cref="IAuditable"/> entity just
/// before EF writes it to the database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cross-cutting concern handled once:</b> no service has to remember to set timestamps,
/// and new auditable entities get the behaviour for free by implementing the interface
/// (Open/Closed Principle).
/// </para>
/// <para>
/// <b>TimeProvider</b> (.NET 8+) replaces <c>DateTime.UtcNow</c>. Tests can inject a fake
/// clock and assert exact timestamps; static calls to DateTime.UtcNow can't be controlled.
/// </para>
/// <para>
/// Registered as a SINGLETON and stateless, which DbContext pooling requires
/// (pooled contexts are shared across requests).
/// </para>
/// </remarks>
internal sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            // Through the change tracker, so the entity keeps private setters.
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAtUtc).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.ModifiedAtUtc).CurrentValue = now;
                    // Guard: an update must never rewrite the creation time.
                    entry.Property(e => e.CreatedAtUtc).IsModified = false;
                    break;
            }
        }
    }
}
