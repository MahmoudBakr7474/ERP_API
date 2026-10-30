using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Application.Contracts.BankReconciliations;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

internal sealed class BankReconciliationReadStore(DbContext db, IBankReconciliationRepository reconciliations) : IBankReconciliationReadStore
{
    public async Task<BankReconciliationResponse?> GetAsync(Guid reconciliationId, CancellationToken ct)
    {
        var reconciliation = await db.Set<BankReconciliation>().AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == reconciliationId, ct);
        if (reconciliation is null)
            return null;
        var bank = await db.Set<CashAccount>().AsNoTracking().SingleAsync(x => x.Id == reconciliation.CashAccountId, ct);
        var ticked = reconciliation.Lines.Select(l => l.JournalLineId).ToHashSet();
        var opening = reconciliation.OpeningClearedBalance ?? await reconciliations.ClearedBalanceAsync(bank.Id, ct);

        // A draft lists every free line up to the statement (its own ticked ones included); a closed one only what it ticked.
        var lines = reconciliation.Status == BankReconciliationStatus.Draft
            ? await reconciliations.FreeLinesAsync(bank.AccountId, reconciliation.StatementDate, reconciliation.Id, null, ct)
            : await reconciliations.FreeLinesAsync(bank.AccountId, reconciliation.StatementDate, reconciliation.Id, ticked, ct);
        var ledger = await (from line in db.Set<JournalLine>()
                            join journal in db.Set<JournalEntry>() on line.JournalEntryId equals journal.Id
                            where line.AccountId == bank.AccountId && journal.Status == JournalStatus.Posted
                                  && journal.PostingDate <= reconciliation.StatementDate
                            select line.Debit - line.Credit).SumAsync(ct);
        return new BankReconciliationResponse(reconciliation.Id, bank.Id, bank.Code, bank.Name, reconciliation.StatementDate,
            reconciliation.StatementBalance, reconciliation.Status, opening, reconciliation.ClearedAmount, reconciliation.Difference(opening), ledger,
            lines.Select(l => new BankLineResponse(l.JournalLineId, l.JournalId, l.JournalNumber, l.PostingDate, l.Description, l.Reference, l.Amount,
                ticked.Contains(l.JournalLineId))).ToList(),
            reconciliation.CompletedAtUtc, Convert.ToBase64String(reconciliation.RowVersion));
    }

    public async Task<IReadOnlyList<BankReconciliationSummaryResponse>> ListAsync(Guid? cashAccountId, CancellationToken ct)
    {
        var query = from reconciliation in db.Set<BankReconciliation>().AsNoTracking()
                    join bank in db.Set<CashAccount>().AsNoTracking() on reconciliation.CashAccountId equals bank.Id
                    select new { reconciliation, bank.Code };
        if (cashAccountId is { } id)
            query = query.Where(x => x.reconciliation.CashAccountId == id);
        return await query.OrderByDescending(x => x.reconciliation.StatementDate).ThenByDescending(x => x.reconciliation.CreatedAtUtc)
            .Select(x => new BankReconciliationSummaryResponse(x.reconciliation.Id, x.reconciliation.CashAccountId, x.Code,
                x.reconciliation.StatementDate, x.reconciliation.StatementBalance, x.reconciliation.Status, x.reconciliation.Lines.Count,
                x.reconciliation.Lines.Sum(l => l.Amount), x.reconciliation.CreatedAtUtc, x.reconciliation.CompletedAtUtc))
            .ToListAsync(ct);
    }
}
