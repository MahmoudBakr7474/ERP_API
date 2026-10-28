namespace WarehouseERP.Inventory.Presentation.Contracts.StockDocuments;

public sealed record RejectStockDocumentRequest(string Reason, string RowVersion);
