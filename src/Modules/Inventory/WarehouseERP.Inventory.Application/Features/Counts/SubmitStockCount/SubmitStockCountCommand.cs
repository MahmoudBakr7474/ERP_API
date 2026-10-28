namespace WarehouseERP.Inventory.Application.Features.Counts.SubmitStockCount;

public sealed record SubmitStockCountCommand(Guid CountId, string RowVersion);
