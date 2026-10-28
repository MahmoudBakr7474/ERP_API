using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.StockDocuments;

/// <param name="Note">The reason of a rejection.</param>
public sealed record StockDocumentEventResponse(StockDocumentAction Action, Guid? UserId, string? UserName, DateTime AtUtc, string? Note);
