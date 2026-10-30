namespace WarehouseERP.Accounting.Presentation.Contracts.BankReconciliations;

/// <summary>The lines cleared on the statement, replacing the earlier selection.</summary>
public sealed record TickBankReconciliationLinesRequest(IReadOnlyList<Guid> JournalLineIds, string RowVersion);
