using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Transfers;

/// <param name="Note">A rejection's reason, or the number of the document a step posted.</param>
public sealed record StockTransferEventResponse(StockTransferAction Action, Guid? UserId, string? UserName, DateTime AtUtc, string? Note);
