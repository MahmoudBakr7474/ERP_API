namespace WarehouseERP.Inventory.Presentation.Contracts.Counts;

public sealed record RejectStockCountRequest(string Reason, string RowVersion);
