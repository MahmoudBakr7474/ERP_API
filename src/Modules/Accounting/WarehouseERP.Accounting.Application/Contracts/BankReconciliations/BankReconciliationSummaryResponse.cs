using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.BankReconciliations;

public sealed record BankReconciliationSummaryResponse(
    Guid Id, Guid CashAccountId, string CashAccountCode, DateOnly StatementDate, decimal StatementBalance, BankReconciliationStatus Status,
    int ClearedLines, decimal ClearedAmount, DateTime CreatedAtUtc, DateTime? CompletedAtUtc);
