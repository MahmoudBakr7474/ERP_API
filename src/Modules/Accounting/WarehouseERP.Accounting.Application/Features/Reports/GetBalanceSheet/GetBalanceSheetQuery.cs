namespace WarehouseERP.Accounting.Application.Features.Reports.GetBalanceSheet;

/// <summary>The company's balance sheet at the end of a date (company level only: transfers move no value between branches).</summary>
public sealed record GetBalanceSheetQuery(DateOnly AsOfDate);
