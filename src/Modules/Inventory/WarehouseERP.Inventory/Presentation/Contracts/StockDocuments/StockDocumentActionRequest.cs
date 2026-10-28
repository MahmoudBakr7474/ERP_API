namespace WarehouseERP.Inventory.Presentation.Contracts.StockDocuments;

/// <summary>Submit, post, approve or cancel: the row version the user saw.</summary>
public sealed record StockDocumentActionRequest(string RowVersion);
