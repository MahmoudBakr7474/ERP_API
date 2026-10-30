using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

internal sealed class SourcePostingRepository(DbContext db) : ISourcePostingRepository
{
    public Task<SourcePosting?> GetForUpdateAsync(Guid sourcePostingId, CancellationToken ct) =>
        db.Set<SourcePosting>().SingleOrDefaultAsync(x => x.Id == sourcePostingId, ct);

    public async Task<IReadOnlySet<Guid>> ExistingAsync(SourceDocumentType sourceType, IReadOnlyCollection<Guid> sourceIds, CancellationToken ct)
    {
        var known = new HashSet<Guid>();
        foreach (var chunk in sourceIds.Distinct().Chunk(1000))
            known.UnionWith(await db.Set<SourcePosting>().AsNoTracking()
                .Where(x => x.SourceType == sourceType && chunk.Contains(x.SourceId))
                .Select(x => x.SourceId)
                .ToListAsync(ct));
        return known;
    }

    public async Task<IReadOnlyList<Guid>> ListDueAsync(DateTime nowUtc, int max, CancellationToken ct) =>
        await db.Set<SourcePosting>().AsNoTracking()
            .Where(x => (x.Status == SourcePostingStatus.Pending || x.Status == SourcePostingStatus.Failed) && x.NextAttemptAtUtc <= nowUtc)
            .OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(max)
            .ToListAsync(ct);

    public Task<SourceCursor?> GetCursorAsync(string module, CancellationToken ct) =>
        db.Set<SourceCursor>().SingleOrDefaultAsync(x => x.Module == module, ct);

    public void Add(SourcePosting posting) => db.Add(posting);

    public void AddCursor(SourceCursor cursor) => db.Add(cursor);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
