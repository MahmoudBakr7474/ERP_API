using WarehouseERP.Inventory.Application.Contracts.Reconciliation;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Reconciliation;

public interface IReconciliationReadStore
{
    /// <summary>Every check, read from one consistent snapshot of the database.</summary>
    Task<ReconciliationReport> GetAsync(DateTime nowUtc, CancellationToken ct);
}
