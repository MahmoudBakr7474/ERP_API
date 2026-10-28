using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts;

// A count belongs to its warehouse's scope; scope comes before existence, as for documents.
internal static class CountAccess
{
    public static async Task<Result<Guid>> AuthorizeAsync(IStockCountRepository counts, ICurrentUser user, Guid countId, CancellationToken ct)
    {
        var warehouseId = await counts.FindWarehouseIdAsync(countId, ct);
        if (warehouseId is null)
            return user.HasAllWarehouses ? StockCountErrors.NotFound : StockCountErrors.AccessDenied;
        if (!user.CanAccessWarehouse(warehouseId.Value))
            return StockCountErrors.AccessDenied;
        return warehouseId.Value;
    }
}
