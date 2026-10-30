namespace WarehouseERP.Accounting.Application.Features.Reports.GetIncomeStatement;

/// <param name="BranchId">Only this branch's revenue and expenses; branch statements add up to the company's.</param>
/// <param name="CompareFromDate">With <paramref name="CompareToDate"/>, a comparison period shown beside each line.</param>
public sealed record GetIncomeStatementQuery(
    DateOnly FromDate, DateOnly ToDate, Guid? BranchId, DateOnly? CompareFromDate, DateOnly? CompareToDate);
