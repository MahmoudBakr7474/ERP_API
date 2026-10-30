using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.BankReconciliations;

/// <summary>
/// A reconciliation with its lines: the ticked ones and, while it is a draft, every other line of the bank's ledger account
/// up to the statement date that no reconciliation holds.
/// </summary>
/// <param name="OpeningClearedBalance">What earlier completed reconciliations cleared.</param>
/// <param name="Difference">Statement balance minus (opening cleared balance + ticked lines); zero to complete.</param>
/// <param name="LedgerBalance">The bank account's posted balance at the end of the statement date.</param>
public sealed record BankReconciliationResponse(
    Guid Id, Guid CashAccountId, string CashAccountCode, string CashAccountName, DateOnly StatementDate, decimal StatementBalance,
    BankReconciliationStatus Status, decimal OpeningClearedBalance, decimal ClearedAmount, decimal Difference, decimal LedgerBalance,
    IReadOnlyList<BankLineResponse> Lines, DateTime? CompletedAtUtc, string RowVersion);
