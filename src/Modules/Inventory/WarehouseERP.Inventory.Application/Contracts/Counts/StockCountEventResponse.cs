using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Counts;

/// <param name="Note">A rejection's reason, or the number of the adjustment the count posted.</param>
public sealed record StockCountEventResponse(StockCountAction Action, Guid? UserId, string? UserName, DateTime AtUtc, string? Note);
