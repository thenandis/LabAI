using Microsoft.EntityFrameworkCore;
using SharpAPI.Application.Abstractions;
using SharpAPI.Application.Common;

namespace SharpAPI.Infrastructure.Persistence;

/// <summary>
/// Commits staged changes and translates EF-specific exceptions into Application ones.
/// </summary>
/// <remarks>
/// This is an ADAPTER (the Hexagonal / Ports-and-Adapters idea): IUnitOfWork is the
/// "port" the Application layer owns, and this class adapts EF Core to it.
/// </remarks>
internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // SaveChanges wraps all pending INSERT/UPDATE/DELETE statements in ONE
            // transaction automatically, so we get atomicity without extra code.
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The record was modified by another user.", ex);
        }
    }
}
