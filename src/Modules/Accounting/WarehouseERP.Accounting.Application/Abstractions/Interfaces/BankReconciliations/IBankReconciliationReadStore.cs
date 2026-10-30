using WarehouseERP.Accounting.Application.Contracts.BankReconciliations;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;

/// <summary>Read side of bank reconciliations.</summary>
public interface IBankReconciliationReadStore
{
    Task<BankReconciliationResponse?> GetAsync(Guid reconciliationId, CancellationToken ct);

    /// <summary>Newest statement first, optionally of one bank account.</summary>
    Task<IReadOnlyList<BankReconciliationSummaryResponse>> ListAsync(Guid? cashAccountId, CancellationToken ct);
}
