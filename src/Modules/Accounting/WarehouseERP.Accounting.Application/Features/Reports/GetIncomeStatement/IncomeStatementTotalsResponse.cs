namespace WarehouseERP.Accounting.Application.Features.Reports.GetIncomeStatement;

/// <summary>The income statement's subtotals: gross, operating and net profit (negative for a loss).</summary>
public sealed record IncomeStatementTotalsResponse(
    decimal Revenue, decimal CostOfSales, decimal GrossProfit, decimal OperatingExpenses, decimal OperatingProfit, decimal OtherIncome,
    decimal OtherExpenses, decimal ProfitBeforeTax, decimal IncomeTax, decimal NetProfit);
