using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;

/// <summary>Write side of bank reconciliations; implemented over the shared ErpDbContext.</summary>
public interface IBankReconciliationRepository
{
    /// <summary>Tracked with its lines, or null; with a row version, the save fails with a conflict unless it still has it.</summary>
    Task<BankReconciliation?> GetForUpdateAsync(Guid reconciliationId, byte[]? expectedRowVersion, CancellationToken ct);

    Task<bool> HasDraftAsync(Guid cashAccountId, CancellationToken ct);

    Task<DateOnly?> LastCompletedStatementDateAsync(Guid cashAccountId, CancellationToken ct);

    /// <summary>What the account's completed reconciliations cleared.</summary>
    Task<decimal> ClearedBalanceAsync(Guid cashAccountId, CancellationToken ct);

    /// <summary>
    /// The posted lines of the ledger account dated on or before the statement that no other reconciliation holds, among
    /// these ids (all of them when null).
    /// </summary>
    Task<IReadOnlyList<BankLine>> FreeLinesAsync(
        Guid accountId, DateOnly statementDate, Guid reconciliationId, IReadOnlyCollection<Guid>? lineIds, CancellationToken ct);

    void Add(BankReconciliation reconciliation);

    /// <summary>The save point; a second draft or a line ticked twice at the same moment comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
