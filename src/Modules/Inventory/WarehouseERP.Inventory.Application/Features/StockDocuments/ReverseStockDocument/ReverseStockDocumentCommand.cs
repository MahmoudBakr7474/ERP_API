namespace WarehouseERP.Inventory.Application.Features.StockDocuments.ReverseStockDocument;

/// <param name="Notes">Why the posted document is undone.</param>
public sealed record ReverseStockDocumentCommand(Guid DocumentId, string? Notes);
