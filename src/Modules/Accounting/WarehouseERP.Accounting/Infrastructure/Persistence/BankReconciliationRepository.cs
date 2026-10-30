using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

internal sealed class BankReconciliationRepository(DbContext db) : IBankReconciliationRepository
{
    public async Task<BankReconciliation?> GetForUpdateAsync(Guid reconciliationId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var reconciliation = await db.Set<BankReconciliation>().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == reconciliationId, ct);
        if (reconciliation is not null && expectedRowVersion is not null)
            db.Entry(reconciliation).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return reconciliation;
    }

    public Task<bool> HasDraftAsync(Guid cashAccountId, CancellationToken ct) =>
        db.Set<BankReconciliation>().AnyAsync(x => x.CashAccountId == cashAccountId && x.Status == BankReconciliationStatus.Draft, ct);

    public async Task<DateOnly?> LastCompletedStatementDateAsync(Guid cashAccountId, CancellationToken ct) =>
        await db.Set<BankReconciliation>()
            .Where(x => x.CashAccountId == cashAccountId && x.Status == BankReconciliationStatus.Completed)
            .Select(x => (DateOnly?)x.StatementDate).MaxAsync(ct);

    public Task<decimal> ClearedBalanceAsync(Guid cashAccountId, CancellationToken ct) =>
        (from line in db.Set<BankReconciliationLine>()
         join reconciliation in db.Set<BankReconciliation>() on line.BankReconciliationId equals reconciliation.Id
         where reconciliation.CashAccountId == cashAccountId && reconciliation.Status == BankReconciliationStatus.Completed
         select line.Amount).SumAsync(ct);

    public async Task<IReadOnlyList<BankLine>> FreeLinesAsync(
        Guid accountId, DateOnly statementDate, Guid reconciliationId, IReadOnlyCollection<Guid>? lineIds, CancellationToken ct)
    {
        var query = from line in db.Set<JournalLine>().AsNoTracking()
                    join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
                    where line.AccountId == accountId && journal.Status == JournalStatus.Posted && journal.PostingDate <= statementDate
                          && !db.Set<BankReconciliationLine>().Any(r => r.JournalLineId == line.Id && r.BankReconciliationId != reconciliationId)
                    select new { line, journal };
        if (lineIds is not null)
        {
            var ids = lineIds.ToArray();
            query = query.Where(x => ids.Contains(x.line.Id));
        }
        return await query.OrderBy(x => x.journal.PostingDate).ThenBy(x => x.journal.Number).ThenBy(x => x.line.LineNumber)
            .Select(x => new BankLine(x.line.Id, x.journal.Id, x.journal.Number!, x.journal.PostingDate, x.line.Description ?? x.journal.Description,
                x.journal.Reference, x.line.Debit - x.line.Credit))
            .ToListAsync(ct);
    }

    public void Add(BankReconciliation reconciliation) => db.Add(reconciliation);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
