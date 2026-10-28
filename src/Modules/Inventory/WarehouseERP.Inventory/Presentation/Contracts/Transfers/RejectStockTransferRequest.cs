namespace WarehouseERP.Inventory.Presentation.Contracts.Transfers;

public sealed record RejectStockTransferRequest(string Reason, string RowVersion);
