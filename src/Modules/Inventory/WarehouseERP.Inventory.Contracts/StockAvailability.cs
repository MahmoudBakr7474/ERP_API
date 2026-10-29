namespace WarehouseERP.Inventory.Contracts;

/// <summary>A product's stock in base units, as a till sees it.</summary>
/// <param name="ProductId">The product.</param>
/// <param name="OnHandSellable">Sellable stock across the warehouse's locations.</param>
/// <param name="ReservedQuantity">What sales orders hold of it.</param>
/// <param name="AtLocation">Available stock in the asked location.</param>
public sealed record StockAvailability(Guid ProductId, decimal OnHandSellable, decimal ReservedQuantity, decimal AtLocation)
{
    /// <summary>What a sale may take: stock nobody reserved in the warehouse, and no more than the location holds.</summary>
    public decimal Sellable => Math.Max(0m, Math.Min(OnHandSellable - ReservedQuantity, AtLocation));
}
