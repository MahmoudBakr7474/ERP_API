using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

internal sealed class PurchaseRequestRepository(DbContext db) : IPurchaseRequestRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid requestId, CancellationToken ct) =>
        db.Set<PurchaseRequest>().AsNoTracking().Where(x => x.Id == requestId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    public async Task<PurchaseRequest?> GetForUpdateAsync(Guid requestId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var request = await db.Set<PurchaseRequest>()
            .FromSqlInterpolated($"SELECT * FROM [purchasing].[PurchaseRequests] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {requestId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (request is not null && expectedRowVersion is not null)
            db.Entry(request).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return request;
    }

    // One request at a time, in id order: two orders drawing on the same requests lock them in the same order.
    public async Task<IReadOnlyList<PurchaseRequest>> GetByLinesForUpdateAsync(IReadOnlyCollection<Guid> requestLineIds, CancellationToken ct)
    {
        var lineIds = requestLineIds.ToArray();
        var requestIds = await db.Set<PurchaseRequestLine>().AsNoTracking()
            .Where(l => lineIds.Contains(l.Id))
            .Select(l => l.RequestId)
            .Distinct()
            .ToListAsync(ct);
        var requests = new List<PurchaseRequest>(requestIds.Count);
        foreach (var requestId in requestIds.Order())
            if (await GetForUpdateAsync(requestId, expectedRowVersion: null, ct) is { } request)
                requests.Add(request);
        return requests;
    }

    public void Add(PurchaseRequest request) => db.Add(request);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PurchasingSaves.SaveAsync(db, ct);
}
