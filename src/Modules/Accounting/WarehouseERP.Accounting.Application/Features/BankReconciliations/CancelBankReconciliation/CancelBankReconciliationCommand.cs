namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.CancelBankReconciliation;

public sealed record CancelBankReconciliationCommand(Guid ReconciliationId, string RowVersion);
