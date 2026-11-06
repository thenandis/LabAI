namespace SharpAPI.Application.Common;

/// <summary>
/// Raised by the Unit of Work when a save fails because the row was changed (or
/// deleted) by someone else since it was loaded.
/// </summary>
/// <remarks>
/// WHY THIS EXISTS: EF Core throws <c>DbUpdateConcurrencyException</c>, but the
/// Application layer must not reference EF Core (Dependency Inversion). Infrastructure
/// therefore translates the EF exception into this persistence-agnostic one, and
/// ProductService turns it into a 409 Conflict result.
/// </remarks>
public sealed class ConcurrencyConflictException(string message, Exception? inner = null)
    : Exception(message, inner);
