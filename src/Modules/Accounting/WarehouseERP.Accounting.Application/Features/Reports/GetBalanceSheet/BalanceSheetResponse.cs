using WarehouseERP.Accounting.Application.Contracts.Reports;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetBalanceSheet;

/// <summary>Assets against liabilities and equity at the end of a date.</summary>
/// <param name="UnclosedEarnings">Revenue minus expenses not yet moved into retained earnings by a year-end close.</param>
/// <param name="IsBalanced">Total assets equal total liabilities and equity.</param>
public sealed record BalanceSheetResponse(
    DateOnly AsOfDate, string CurrencyCode, IReadOnlyList<StatementLineResponse> Lines, decimal CurrentAssets, decimal NonCurrentAssets,
    decimal TotalAssets, decimal CurrentLiabilities, decimal NonCurrentLiabilities, decimal TotalLiabilities, decimal Equity,
    decimal UnclosedEarnings, decimal TotalEquity, decimal TotalLiabilitiesAndEquity, bool IsBalanced);
