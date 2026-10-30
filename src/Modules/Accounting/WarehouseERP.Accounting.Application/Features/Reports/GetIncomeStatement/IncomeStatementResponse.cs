using WarehouseERP.Accounting.Application.Contracts.Reports;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetIncomeStatement;

/// <summary>Revenue and expenses over a date range, without year-end closing journals.</summary>
public sealed record IncomeStatementResponse(
    DateOnly FromDate, DateOnly ToDate, Guid? BranchId, DateOnly? CompareFromDate, DateOnly? CompareToDate, string CurrencyCode,
    IReadOnlyList<StatementLineResponse> Lines, IncomeStatementTotalsResponse Totals, IncomeStatementTotalsResponse? ComparativeTotals);
