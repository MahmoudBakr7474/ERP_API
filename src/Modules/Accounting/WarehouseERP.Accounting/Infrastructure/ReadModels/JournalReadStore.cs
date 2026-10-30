using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

internal sealed class JournalReadStore(DbContext db, IUserDirectory users, IBranchDirectory branches) : IJournalReadStore
{
    public async Task<JournalResponse?> GetAsync(Guid journalId, CancellationToken ct)
    {
        var journal = await db.Set<JournalEntry>().AsNoTracking().Include(x => x.Lines).Include(x => x.Events).AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == journalId, ct);
        if (journal is null)
            return null;

        var reversalOfNumber = journal.ReversalOfJournalId is { } reversedId
            ? await db.Set<JournalEntry>().Where(x => x.Id == reversedId).Select(x => x.Number).SingleOrDefaultAsync(ct)
            : null;
        var reversedBy = await db.Set<JournalEntry>().AsNoTracking()
            .Where(x => x.ReversalOfJournalId == journalId && x.Status != JournalStatus.Cancelled)
            .Select(x => new { x.Id, x.Number, x.Status }).SingleOrDefaultAsync(ct);
        var period = journal.FiscalPeriodId is { } periodId
            ? await (from p in db.Set<FiscalPeriod>()
                     join y in db.Set<FiscalYear>() on p.FiscalYearId equals y.Id
                     where p.Id == periodId
                     select new { YearName = y.Name, p.Number }).SingleOrDefaultAsync(ct)
            : null;

        var accountIds = journal.Lines.Select(l => l.AccountId).Distinct().ToArray();
        var accounts = await db.Set<Account>().AsNoTracking().Where(x => accountIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new { x.Code, x.NameAr, x.NameEn }, ct);
        var branchInfo = (await branches.FindManyAsync(journal.Lines.Select(l => l.BranchId).Distinct().ToList(), ct)).ToDictionary(b => b.Id);
        var userIds = journal.Events.Select(e => e.UserId)
            .Append(journal.CreatedByUserId).Append(journal.ApprovedByUserId).Append(journal.PostedByUserId)
            .Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        IReadOnlyDictionary<Guid, string> names = userIds.Count == 0 ? new Dictionary<Guid, string>() : await users.GetDisplayNamesAsync(userIds, ct);
        string? Name(Guid? userId) => userId is { } id ? names.GetValueOrDefault(id) : null;

        var lines = journal.Lines.OrderBy(l => l.LineNumber).Select(l =>
        {
            var account = accounts.GetValueOrDefault(l.AccountId);
            var branch = branchInfo.GetValueOrDefault(l.BranchId);
            return new JournalLineResponse(l.LineNumber, l.AccountId, account?.Code ?? "", account?.NameAr ?? "", account?.NameEn, l.BranchId,
                branch?.Code, branch?.NameAr, l.Debit, l.Credit, l.Description);
        }).ToList();
        var events = journal.Events.OrderBy(e => e.Id)
            .Select(e => new JournalEventResponse(e.Action, e.UserId, Name(e.UserId), e.AtUtc, e.Note)).ToList();

        return new JournalResponse(journal.Id, JournalNumbers.DraftReference(journal.DraftNumber), journal.Number, journal.JournalType,
            journal.Status, journal.PostingDate, period?.YearName, period?.Number, journal.Description, journal.Reference, journal.CurrencyCode,
            journal.RequiresApproval, journal.LinesFixed, journal.TotalDebit, journal.TotalCredit, journal.ReversalOfJournalId, reversalOfNumber,
            reversedBy?.Id, reversedBy?.Number, reversedBy?.Status, journal.CreatedByUserId, Name(journal.CreatedByUserId), journal.CreatedAtUtc,
            journal.ApprovedByUserId, Name(journal.ApprovedByUserId), journal.PostedByUserId, Name(journal.PostedByUserId), journal.PostedAtUtc,
            lines, events, Convert.ToBase64String(journal.RowVersion));
    }

    public async Task<PagedResult<JournalSummaryResponse>> ListAsync(JournalFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<JournalEntry>().AsNoTracking();
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.Type is { } type)
            query = query.Where(x => x.JournalType == type);
        if (filter.FromDate is { } from)
            query = query.Where(x => x.PostingDate >= from);
        if (filter.ToDate is { } to)
            query = query.Where(x => x.PostingDate <= to);
        if (filter.Search is { } search)
        {
            var draft = search.StartsWith("DJ-", StringComparison.OrdinalIgnoreCase) && int.TryParse(search[3..], out var n) ? n : (int?)null;
            query = query.Where(x => (x.Number != null && x.Number.Contains(search)) || x.Description.Contains(search)
                || (x.Reference != null && x.Reference.Contains(search)) || (draft != null && x.DraftNumber == draft));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.PostingDate).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.DraftNumber, x.Number, x.JournalType, x.Status, x.PostingDate, x.Description, x.Reference, x.TotalDebit,
                LineCount = x.Lines.Count, x.ReversalOfJournalId, x.CreatedAtUtc, x.PostedAtUtc,
            })
            .ToListAsync(ct);
        var items = rows.Select(x => new JournalSummaryResponse(x.Id, JournalNumbers.DraftReference(x.DraftNumber), x.Number, x.JournalType,
            x.Status, x.PostingDate, x.Description, x.Reference, x.TotalDebit, x.LineCount, x.ReversalOfJournalId, x.CreatedAtUtc,
            x.PostedAtUtc)).ToList();
        return new PagedResult<JournalSummaryResponse>(items, page, pageSize, total);
    }

    public async Task<JournalPostedResponse?> FindPostedAsync(Guid journalId, CancellationToken ct) =>
        await db.Set<JournalEntry>().AsNoTracking()
            .Where(x => x.Id == journalId && x.Status == JournalStatus.Posted)
            .Select(x => new JournalPostedResponse(x.Id, x.Number!, x.JournalType, x.PostingDate, x.TotalDebit, x.PostedAtUtc!.Value))
            .SingleOrDefaultAsync(ct);
}
