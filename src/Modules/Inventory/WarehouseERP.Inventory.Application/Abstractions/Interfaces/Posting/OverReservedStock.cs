namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

public sealed record OverReservedStock(Guid ProductId, Guid WarehouseId, decimal OnHandSellable, decimal ReservedQuantity);
