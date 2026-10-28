using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;

/// <summary>Write side of transfers with their lines and history (one aggregate).</summary>
public interface IStockTransferRepository
{
    /// <summary>What decides who may touch the transfer, or null when no transfer has this id.</summary>
    Task<StockTransferHeader?> FindHeaderAsync(Guid transferId, CancellationToken ct);

    /// <summary>
    /// The transfer with its lines, tracked and locked against other writers until the transaction ends, or null. With a
    /// row version, the save fails with a conflict unless the transfer still has it.
    /// </summary>
    Task<StockTransfer?> GetForUpdateAsync(Guid transferId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(StockTransfer transfer);

    /// <summary>The save point; a transfer whose lines or history changed is saved as changed, so its row version moves.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
