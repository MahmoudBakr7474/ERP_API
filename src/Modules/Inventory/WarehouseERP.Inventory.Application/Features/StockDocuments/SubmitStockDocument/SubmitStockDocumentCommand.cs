namespace WarehouseERP.Inventory.Application.Features.StockDocuments.SubmitStockDocument;

public sealed record SubmitStockDocumentCommand(Guid DocumentId, string RowVersion);
