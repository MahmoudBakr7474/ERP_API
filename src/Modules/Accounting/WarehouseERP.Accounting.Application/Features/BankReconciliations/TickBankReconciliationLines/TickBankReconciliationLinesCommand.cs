namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.TickBankReconciliationLines;

/// <summary>The lines cleared on the statement, replacing the earlier selection.</summary>
public sealed record TickBankReconciliationLinesCommand(Guid ReconciliationId, IReadOnlyList<Guid> JournalLineIds, string RowVersion);
