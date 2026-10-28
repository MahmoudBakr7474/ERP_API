namespace WarehouseERP.Inventory.Application.Features.StockDocuments.CancelStockDocument;

public sealed record CancelStockDocumentCommand(Guid DocumentId, string RowVersion);
