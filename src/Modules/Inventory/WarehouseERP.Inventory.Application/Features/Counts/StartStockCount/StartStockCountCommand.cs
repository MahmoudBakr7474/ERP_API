namespace WarehouseERP.Inventory.Application.Features.Counts.StartStockCount;

public sealed record StartStockCountCommand(Guid CountId, string RowVersion);
