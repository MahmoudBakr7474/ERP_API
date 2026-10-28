using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers;

// Who may touch a transfer: the source warehouse's people prepare and dispatch it, the destination's receive it, either
// side may read it or close it. Scope comes before existence, as for documents.
internal static class TransferAccess
{
    public enum Side
    {
        Source,
        Destination,
        Either,
    }

    public static async Task<Result<StockTransferHeader>> AuthorizeAsync(
        IStockTransferRepository transfers, ICurrentUser user, Guid transferId, Side side, CancellationToken ct)
    {
        var header = await transfers.FindHeaderAsync(transferId, ct);
        if (header is null)
            return user.HasAllWarehouses ? StockTransferErrors.NotFound : StockTransferErrors.AccessDenied;
        var allowed = side switch
        {
            Side.Source => user.CanAccessWarehouse(header.SourceWarehouseId),
            Side.Destination => user.CanAccessWarehouse(header.DestinationWarehouseId),
            _ => user.CanAccessWarehouse(header.SourceWarehouseId) || user.CanAccessWarehouse(header.DestinationWarehouseId),
        };
        if (!allowed)
            return StockTransferErrors.AccessDenied;
        return header;
    }
}
