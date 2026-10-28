using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

internal sealed class GoodsReceiptRepository(DbContext db) : IGoodsReceiptRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid receiptId, CancellationToken ct) =>
        db.Set<GoodsReceipt>().AsNoTracking().Where(x => x.Id == receiptId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    public Task<GoodsReceipt?> GetForUpdateAsync(Guid receiptId, CancellationToken ct) =>
        db.Set<GoodsReceipt>()
            .FromSqlInterpolated($"SELECT * FROM [purchasing].[GoodsReceipts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {receiptId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);

    public void Add(GoodsReceipt receipt) => db.Add(receipt);
}
