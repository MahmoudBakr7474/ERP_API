using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

// Posted lines joined to their journal; sums are computed by SQL Server from the lines every time (no stored balances).
internal sealed class LedgerReadStore(DbContext db) : ILedgerReadStore
{
    public async Task<IReadOnlyList<AccountTotals>> TotalsAsync(
        DateOnly? fromDate, DateOnly toDate, Guid? branchId, bool excludeYearEndClosing, CancellationToken ct)
    {
        var lines = Posted();
        if (fromDate is { } from)
            lines = lines.Where(x => x.Journal.PostingDate >= from);
        lines = lines.Where(x => x.Journal.PostingDate <= toDate);
        if (branchId is { } branch)
            lines = lines.Where(x => x.Line.BranchId == branch);
        if (excludeYearEndClosing)
            lines = lines.Where(x => x.Journal.JournalType != JournalType.YearEndClosing);
        return await lines.GroupBy(x => x.Line.AccountId)
            .Select(g => new AccountTotals(g.Key, g.Sum(x => x.Line.Debit), g.Sum(x => x.Line.Credit)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AccountBranchTotals>> IncomeStatementTotalsByBranchAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct) =>
        await (from row in Posted()
               join account in db.Set<Account>() on row.Line.AccountId equals account.Id
               where row.Journal.PostingDate >= fromDate && row.Journal.PostingDate <= toDate
                     && (account.Type == AccountType.Revenue || account.Type == AccountType.Expense)
               group row.Line by new { row.Line.AccountId, row.Line.BranchId } into g
               select new AccountBranchTotals(g.Key.AccountId, g.Key.BranchId, g.Sum(x => x.Debit), g.Sum(x => x.Credit)))
            .ToListAsync(ct);

    public Task<int> CountLinesAsync(LedgerLineFilter filter, CancellationToken ct) => Filtered(filter).CountAsync(ct);

    public async Task<IReadOnlyList<LedgerLine>> LinesAsync(LedgerLineFilter filter, int max, CancellationToken ct) =>
        await Project(Ordered(Filtered(filter)).Take(max)).ToListAsync(ct);

    public async Task<PagedResult<LedgerLine>> PageLinesAsync(LedgerLineFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var lines = Filtered(filter);
        var total = await lines.CountAsync(ct);
        var items = await Project(Ordered(lines).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return new PagedResult<LedgerLine>(items, page, pageSize, total);
    }

    private IQueryable<PostedLine> Posted() =>
        from line in db.Set<JournalLine>().AsNoTracking()
        join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
        where journal.Status == JournalStatus.Posted
        select new PostedLine { Line = line, Journal = journal };

    private IQueryable<PostedLine> Filtered(LedgerLineFilter filter)
    {
        var lines = Posted().Where(x => x.Journal.PostingDate >= filter.FromDate && x.Journal.PostingDate <= filter.ToDate);
        if (filter.AccountIds is { } accountIds)
        {
            var ids = accountIds.ToArray();
            lines = lines.Where(x => ids.Contains(x.Line.AccountId));
        }
        if (filter.BranchId is { } branchId)
            lines = lines.Where(x => x.Line.BranchId == branchId);
        if (filter.JournalType is { } type)
            lines = lines.Where(x => x.Journal.JournalType == type);
        return lines;
    }

    private static IQueryable<PostedLine> Ordered(IQueryable<PostedLine> lines) =>
        lines.OrderBy(x => x.Journal.PostingDate).ThenBy(x => x.Journal.Number).ThenBy(x => x.Line.LineNumber);

    private static IQueryable<LedgerLine> Project(IQueryable<PostedLine> lines) =>
        lines.Select(x => new LedgerLine(x.Journal.Id, x.Journal.Number!, x.Journal.JournalType, x.Journal.PostingDate, x.Journal.Description,
            x.Journal.Reference, x.Line.LineNumber, x.Line.AccountId, x.Line.BranchId, x.Line.Debit, x.Line.Credit, x.Line.Description));

    // A member-initialised shape (not a constructor) so EF Core can keep composing filters and groupings over it.
    private sealed class PostedLine
    {
        public JournalLine Line { get; init; } = null!;
        public JournalEntry Journal { get; init; } = null!;
    }
}
