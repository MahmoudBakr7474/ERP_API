using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

internal sealed class JournalRepository(DbContext db) : IJournalRepository
{
    public Task<JournalEntry?> FindAsync(Guid journalId, CancellationToken ct) =>
        db.Set<JournalEntry>().AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == journalId, ct);

    public async Task<JournalEntry?> GetForUpdateAsync(Guid journalId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var journal = await db.Set<JournalEntry>().Include(x => x.Lines).Include(x => x.Allocations).AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == journalId, ct);
        if (journal is not null && expectedRowVersion is not null)
            db.Entry(journal).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return journal;
    }

    public Task<bool> HasActiveReversalAsync(Guid journalId, CancellationToken ct) =>
        db.Set<JournalEntry>().AnyAsync(x => x.ReversalOfJournalId == journalId && x.Status != JournalStatus.Cancelled, ct);

    public Task<bool> AnyPostedAsync(CancellationToken ct) => db.Set<JournalEntry>().AnyAsync(x => x.Status == JournalStatus.Posted, ct);

    public Task<int> CountOpenAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        db.Set<JournalEntry>().CountAsync(x => (x.Status == JournalStatus.Draft || x.Status == JournalStatus.Submitted)
            && x.PostingDate >= from && x.PostingDate <= to, ct);

    public Task<int> CountOpenOfTypeAsync(JournalType type, CancellationToken ct) =>
        db.Set<JournalEntry>().CountAsync(x => x.JournalType == type && (x.Status == JournalStatus.Draft || x.Status == JournalStatus.Submitted), ct);

    public void Add(JournalEntry journal) => db.Add(journal);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
