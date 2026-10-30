namespace WarehouseERP.Accounting.Application.Features.Reports.GetTrialBalance;

/// <summary>The trial balance; totals are over posting accounts, so nothing is counted twice through its headers.</summary>
/// <param name="IsBalanced">Every column pair equal; always true company-wide, not necessarily for one branch.</param>
public sealed record TrialBalanceResponse(
    DateOnly FromDate, DateOnly ToDate, Guid? BranchId, int? Level, string CurrencyCode, IReadOnlyList<TrialBalanceRowResponse> Rows,
    decimal OpeningDebit, decimal OpeningCredit, decimal PeriodDebit, decimal PeriodCredit, decimal ClosingDebit, decimal ClosingCredit,
    bool IsBalanced);
