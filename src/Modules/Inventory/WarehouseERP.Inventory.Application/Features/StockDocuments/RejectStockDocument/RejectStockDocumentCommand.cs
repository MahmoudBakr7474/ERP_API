namespace WarehouseERP.Inventory.Application.Features.StockDocuments.RejectStockDocument;

/// <param name="Reason">Why it goes back to draft; kept in the document's history.</param>
public sealed record RejectStockDocumentCommand(Guid DocumentId, string Reason, string RowVersion);
