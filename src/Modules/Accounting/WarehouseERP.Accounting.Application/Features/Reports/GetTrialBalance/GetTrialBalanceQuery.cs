namespace WarehouseERP.Accounting.Application.Features.Reports.GetTrialBalance;

/// <param name="BranchId">Only lines of this branch; a branch's trial balance needn't balance (a journal may span branches).</param>
/// <param name="Level">Show accounts down to this level, deeper accounts summed into them; null shows every account.</param>
/// <param name="IncludeZero">Also list accounts with nothing in any column.</param>
public sealed record GetTrialBalanceQuery(DateOnly FromDate, DateOnly ToDate, Guid? BranchId, int? Level, bool IncludeZero);
