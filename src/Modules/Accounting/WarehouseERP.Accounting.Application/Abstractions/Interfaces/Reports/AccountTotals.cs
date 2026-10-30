namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;

/// <summary>Posted debits and credits of one account over a date range.</summary>
public sealed record AccountTotals(Guid AccountId, decimal Debit, decimal Credit);
