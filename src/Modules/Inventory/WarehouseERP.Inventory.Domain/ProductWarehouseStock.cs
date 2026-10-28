namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// Sellable stock and reservations of a product in one physical warehouse: the serialization point of every operation
/// that can reduce availability. OnHandSellable is the available stock across all its locations; transit warehouses
/// have no row. Reserved may exceed on hand after an authorized loss (recorded as an over-reservation alert); neither
/// goes below zero.
/// </summary>
public sealed class ProductWarehouseStock
{
    private ProductWarehouseStock() { } // EF Core

    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal OnHandSellable { get; private set; }

    /// <summary>Stays 0 until Sales/POS reservations exist.</summary>
    public decimal ReservedQuantity { get; private set; }

    public byte[] RowVersion { get; private set; } = [];
}
