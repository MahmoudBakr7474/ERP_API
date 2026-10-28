namespace WarehouseERP.Inventory.Application.Features.StockDocuments.ApproveStockDocument;

/// <param name="IdempotencyKey">Chosen by the client for this click and sent again on a retry.</param>
/// <param name="RowVersion">The version the approver saw; a changed document is not posted (409).</param>
public sealed record ApproveStockDocumentCommand(Guid DocumentId, Guid IdempotencyKey, string RowVersion);
