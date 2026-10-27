using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;

/// <summary>The module's save point: one SaveChanges for everything the use case changed, one transaction.</summary>
public interface IIdentityPersistence
{
    Task<Result> SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Forgets every pending change after a failed save, so a use case can reload and save a different outcome
    /// (e.g. revoke a session after a conflicting refresh) instead of retrying the failed changes.
    /// </summary>
    void DiscardChanges();
}
