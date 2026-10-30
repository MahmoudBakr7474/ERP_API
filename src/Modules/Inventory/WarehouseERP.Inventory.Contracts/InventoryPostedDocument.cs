namespace WarehouseERP.Inventory.Contracts;

/// <summary>A posted stock document as Accounting discovers it.</summary>
public sealed record InventoryPostedDocument(Guid DocumentId, StockDocumentType DocumentType, string Number, DateTime PostedAtUtc);
