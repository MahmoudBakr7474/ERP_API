using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;

/// <summary>Write side of goods receipts; saved by the order's save point in the same transaction.</summary>
public interface IGoodsReceiptRepository
{
    Task<Guid?> FindWarehouseAsync(Guid receiptId, CancellationToken ct);

    /// <summary>The receipt with its lines, tracked, or null.</summary>
    Task<GoodsReceipt?> GetForUpdateAsync(Guid receiptId, CancellationToken ct);

    /// <summary>Tracks a new receipt and gives it its id, which its stock document refers to.</summary>
    void Add(GoodsReceipt receipt);
}
