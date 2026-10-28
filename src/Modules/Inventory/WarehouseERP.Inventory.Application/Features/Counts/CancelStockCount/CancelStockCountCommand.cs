namespace WarehouseERP.Inventory.Application.Features.Counts.CancelStockCount;

public sealed record CancelStockCountCommand(Guid CountId, string RowVersion);
