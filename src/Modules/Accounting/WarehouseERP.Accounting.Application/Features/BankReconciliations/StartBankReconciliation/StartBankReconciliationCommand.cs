namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.StartBankReconciliation;

/// <param name="StatementBalance">The statement's closing balance (money in the bank positive).</param>
public sealed record StartBankReconciliationCommand(Guid CashAccountId, DateOnly StatementDate, decimal StatementBalance);
