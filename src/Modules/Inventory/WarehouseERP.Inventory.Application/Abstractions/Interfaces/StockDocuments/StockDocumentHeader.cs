using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;

/// <param name="ReversedType">For a reversal, the type of the document it reverses (its permission applies).</param>
public sealed record StockDocumentHeader(Guid Id, Guid WarehouseId, StockDocumentType DocumentType, StockDocumentType? ReversedType);
