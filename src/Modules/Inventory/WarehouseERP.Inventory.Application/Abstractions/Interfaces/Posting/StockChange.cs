namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <summary>One side of an allocation: a signed base quantity in one location.</summary>
/// <param name="Sellable">True in a physical warehouse, whose available stock counts toward OnHandSellable.</param>
public sealed record StockChange(
    Guid LineId, Guid AllocationId, Guid ProductId, Guid WarehouseId, Guid LocationId, decimal Quantity, bool Sellable);
