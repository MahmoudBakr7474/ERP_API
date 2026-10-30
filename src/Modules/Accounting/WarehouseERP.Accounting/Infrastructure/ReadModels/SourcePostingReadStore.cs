using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Application.Contracts.SourcePostings;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

internal sealed class SourcePostingReadStore(DbContext db) : ISourcePostingReadStore
{
    private static readonly Expression<Func<SourcePosting, SourcePostingResponse>> ToResponse = x => new SourcePostingResponse(
        x.Id, x.SourceType, x.SourceId, x.SourceNumber, x.OccurredAtUtc, x.PostingDate, x.Status, x.JournalId, x.JournalNumber, x.Attempts,
        x.LastAttemptAtUtc, x.NextAttemptAtUtc, x.ErrorCode, x.ErrorMessage, x.StockValue, x.PostedStockValue, x.VarianceAmount,
        x.DiscoveredAtUtc, x.PostedAtUtc);

    public async Task<PagedResult<SourcePostingResponse>> ListAsync(SourcePostingFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SourcePosting>().AsNoTracking();
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.SourceType is { } sourceType)
            query = query.Where(x => x.SourceType == sourceType);
        if (filter.FromDate is { } from)
            query = query.Where(x => x.PostingDate >= from);
        if (filter.ToDate is { } to)
            query = query.Where(x => x.PostingDate <= to);
        if (filter.VarianceOnly)
            query = query.Where(x => x.VarianceAmount != null);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(ToResponse).ToListAsync(ct);
        return new PagedResult<SourcePostingResponse>(items, page, pageSize, total);
    }

    public Task<SourcePostingResponse?> FindAsync(Guid sourcePostingId, CancellationToken ct) =>
        db.Set<SourcePosting>().AsNoTracking().Where(x => x.Id == sourcePostingId).Select(ToResponse).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SourceStatusCount>> CountsAsync(CancellationToken ct) =>
        await db.Set<SourcePosting>().AsNoTracking()
            .GroupBy(x => new { x.SourceType, x.Status })
            .OrderBy(g => g.Key.SourceType).ThenBy(g => g.Key.Status)
            .Select(g => new SourceStatusCount(g.Key.SourceType, g.Key.Status, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SourceCursorResponse>> CursorsAsync(CancellationToken ct) =>
        await db.Set<SourceCursor>().AsNoTracking().OrderBy(x => x.Module)
            .Select(x => new SourceCursorResponse(x.Module, x.ScannedToUtc, x.UpdatedAtUtc))
            .ToListAsync(ct);

    public Task<int> CountUnsettledAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        db.Set<SourcePosting>().CountAsync(x => (x.Status == SourcePostingStatus.Pending || x.Status == SourcePostingStatus.Failed)
                                                && x.PostingDate >= from && x.PostingDate <= to, ct);

    public Task<DateTime?> OldestUnsettledAsync(CancellationToken ct) =>
        db.Set<SourcePosting>().AsNoTracking()
            .Where(x => x.Status == SourcePostingStatus.Pending || x.Status == SourcePostingStatus.Failed)
            .MinAsync(x => (DateTime?)x.OccurredAtUtc, ct);

    public async Task<IReadOnlySet<(SourceDocumentType Type, Guid Id)>> RegisteredAsync(CancellationToken ct) =>
        (await db.Set<SourcePosting>().AsNoTracking().Select(x => new { x.SourceType, x.SourceId }).ToListAsync(ct))
        .Select(x => (x.SourceType, x.SourceId)).ToHashSet();

    public async Task<SourceStockTotals> StockTotalsAsync(CancellationToken ct)
    {
        var totals = await db.Set<SourcePosting>().AsNoTracking()
            .Where(x => (x.Status == SourcePostingStatus.Posted || x.Status == SourcePostingStatus.NothingToPost) && x.StockValue != null)
            .GroupBy(_ => 1)
            .Select(g => new { Stock = g.Sum(x => x.StockValue!.Value), Posted = g.Sum(x => x.PostedStockValue ?? 0m) })
            .SingleOrDefaultAsync(ct);
        return new SourceStockTotals(totals?.Stock ?? 0m, totals?.Posted ?? 0m);
    }

    public async Task<int> UnmatchedJournalsAsync(CancellationToken ct)
    {
        var journals = await (
                from journal in db.Set<JournalEntry>().AsNoTracking()
                where journal.SourceId != null
                join posting in db.Set<SourcePosting>().AsNoTracking()
                    on new { Type = journal.SourceType!.Value, Id = journal.SourceId!.Value } equals new { Type = posting.SourceType, Id = posting.SourceId }
                    into postings
                from posting in postings.DefaultIfEmpty()
                where posting == null || posting.JournalId != journal.Id || posting.Status != SourcePostingStatus.Posted
                select journal.Id)
            .CountAsync(ct);
        var postingsWithoutJournal = await (
                from posting in db.Set<SourcePosting>().AsNoTracking()
                where posting.Status == SourcePostingStatus.Posted
                join journal in db.Set<JournalEntry>().AsNoTracking() on posting.JournalId equals journal.Id into found
                from journal in found.DefaultIfEmpty()
                where journal == null || journal.SourceId != posting.SourceId || journal.SourceType != posting.SourceType
                select posting.Id)
            .CountAsync(ct);
        return journals + postingsWithoutJournal;
    }
}
