namespace WarehouseERP.Inventory.Application.Features.StockDocuments.PostStockDocument;

/// <param name="IdempotencyKey">Chosen by the client for this click and sent again on a retry.</param>
/// <param name="RowVersion">The version the user saw; a changed document is not posted (409).</param>
public sealed record PostStockDocumentCommand(Guid DocumentId, Guid IdempotencyKey, string RowVersion);
