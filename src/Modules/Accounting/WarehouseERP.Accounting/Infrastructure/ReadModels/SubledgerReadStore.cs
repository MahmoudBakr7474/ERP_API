using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

// The subledger read from its own tables: open items and their allocations, and the posted control-account lines.
internal sealed class SubledgerReadStore(DbContext db) : ISubledgerReadStore
{
    public async Task<IReadOnlyList<AgingItem>> OpenItemsAsOfAsync(PartyType partyType, DateOnly asOfDate, Guid? branchId, CancellationToken ct)
    {
        var items = db.Set<OpenItem>().AsNoTracking().Where(x => x.PartyType == partyType && x.DocumentDate <= asOfDate);
        if (branchId is { } branch)
            items = items.Where(x => x.BranchId == branch);
        var rows = await items.Select(x => new
        {
            x.PartyId,
            x.AccountId,
            x.DueDate,
            x.Amount,
            Settled = db.Set<Allocation>()
                .Where(a => a.IsActive && a.AllocatedOn <= asOfDate && (a.DebitItemId == x.Id || a.CreditItemId == x.Id))
                .Sum(a => (decimal?)a.Amount) ?? 0m,
        }).ToListAsync(ct);
        return rows.Select(r => new AgingItem(partyType, r.PartyId, r.AccountId, r.DueDate, r.Amount > 0m ? r.Amount - r.Settled : r.Amount + r.Settled))
            .Where(i => i.OpenAmount != 0m)
            .ToList();
    }

    public Task<decimal> PartyBalanceBeforeAsync(PartyType partyType, Guid partyId, DateOnly date, CancellationToken ct) =>
        (from line in db.Set<JournalLine>()
         join journal in db.Set<JournalEntry>() on line.JournalEntryId equals journal.Id
         where line.PartyType == partyType && line.PartyId == partyId && journal.Status == JournalStatus.Posted && journal.PostingDate < date
         select line.Debit - line.Credit).SumAsync(ct);

    public async Task<IReadOnlyList<LedgerLine>> PartyLinesAsync(
        PartyType partyType, Guid partyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct) =>
        await (from line in db.Set<JournalLine>().AsNoTracking()
               join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
               where line.PartyType == partyType && line.PartyId == partyId && journal.Status == JournalStatus.Posted
                     && journal.PostingDate >= fromDate && journal.PostingDate <= toDate
               orderby journal.PostingDate, journal.Number, line.LineNumber
               select new LedgerLine(journal.Id, journal.Number!, journal.JournalType, journal.PostingDate, journal.Description, journal.Reference,
                   line.LineNumber, line.AccountId, line.BranchId, line.Debit, line.Credit, line.Description))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ControlAccountTotals>> ControlTotalsAsync(DateOnly asOfDate, CancellationToken ct)
    {
        var controls = await db.Set<Account>().AsNoTracking().Where(a => a.PartyType != null).Select(a => a.Id).ToListAsync(ct);
        if (controls.Count == 0)
            return [];
        var ledger = await (from line in db.Set<JournalLine>()
                            join journal in db.Set<JournalEntry>() on line.JournalEntryId equals journal.Id
                            where controls.Contains(line.AccountId) && journal.Status == JournalStatus.Posted && journal.PostingDate <= asOfDate
                            group line by line.AccountId into g
                            select new
                            {
                                AccountId = g.Key,
                                Balance = g.Sum(l => l.Debit - l.Credit),
                                WithoutParty = g.Where(l => l.PartyId == null).Sum(l => (decimal?)(l.Debit - l.Credit)) ?? 0m,
                            }).ToDictionaryAsync(x => x.AccountId, ct);
        var items = await (from item in db.Set<OpenItem>()
                           where controls.Contains(item.AccountId) && item.DocumentDate <= asOfDate
                           select new
                           {
                               item.AccountId,
                               item.Amount,
                               Settled = db.Set<Allocation>()
                                   .Where(a => a.IsActive && a.AllocatedOn <= asOfDate && (a.DebitItemId == item.Id || a.CreditItemId == item.Id))
                                   .Sum(a => (decimal?)a.Amount) ?? 0m,
                           }).ToListAsync(ct);
        return controls.Select(id =>
        {
            var account = items.Where(i => i.AccountId == id).ToList();
            var open = account.Sum(i => i.Amount > 0m ? i.Amount - i.Settled : i.Amount + i.Settled);
            return new ControlAccountTotals(id, ledger.GetValueOrDefault(id)?.Balance ?? 0m, ledger.GetValueOrDefault(id)?.WithoutParty ?? 0m,
                account.Sum(i => i.Amount), open);
        }).ToList();
    }
}
