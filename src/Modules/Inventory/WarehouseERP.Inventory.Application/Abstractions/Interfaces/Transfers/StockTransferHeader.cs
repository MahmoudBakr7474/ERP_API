namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;

public sealed record StockTransferHeader(Guid Id, Guid SourceWarehouseId, Guid DestinationWarehouseId);
