namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;

/// <summary>Posted debits and credits of one account in one branch over a date range.</summary>
public sealed record AccountBranchTotals(Guid AccountId, Guid BranchId, decimal Debit, decimal Credit);
