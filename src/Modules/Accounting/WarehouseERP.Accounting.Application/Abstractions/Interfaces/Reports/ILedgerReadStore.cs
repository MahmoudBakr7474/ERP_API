using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;

/// <summary>
/// The ledger: posted journal lines only (drafts, submitted and cancelled journals never count), summed or listed by
/// posting date. Balances are always computed from the lines; nothing else stores them.
/// </summary>
public interface ILedgerReadStore
{
    /// <summary>
    /// Totals per account of lines dated from <paramref name="fromDate"/> (null: from the beginning) to
    /// <paramref name="toDate"/>, optionally in one branch, optionally leaving year-end closing journals out.
    /// </summary>
    Task<IReadOnlyList<AccountTotals>> TotalsAsync(
        DateOnly? fromDate, DateOnly toDate, Guid? branchId, bool excludeYearEndClosing, CancellationToken ct);

    /// <summary>Totals per account and branch of revenue and expense accounts over the dates (what a year-end close moves).</summary>
    Task<IReadOnlyList<AccountBranchTotals>> IncomeStatementTotalsByBranchAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct);

    Task<int> CountLinesAsync(LedgerLineFilter filter, CancellationToken ct);

    /// <summary>The lines in date, number and line order, at most <paramref name="max"/>.</summary>
    Task<IReadOnlyList<LedgerLine>> LinesAsync(LedgerLineFilter filter, int max, CancellationToken ct);

    Task<PagedResult<LedgerLine>> PageLinesAsync(LedgerLineFilter filter, int page, int pageSize, CancellationToken ct);
}
