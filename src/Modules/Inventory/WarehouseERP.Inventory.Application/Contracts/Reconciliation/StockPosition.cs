namespace WarehouseERP.Inventory.Application.Contracts.Reconciliation;

/// <param name="PhysicalQuantity">In physical warehouses.</param>
/// <param name="InTransitQuantity">In transit locations.</param>
/// <param name="ValuedQuantity">What the valuation holds; equals physical plus in transit when consistent.</param>
public sealed record StockPosition(
    Guid ProductId, string Sku, string ProductNameAr, decimal PhysicalQuantity, decimal InTransitQuantity, decimal ValuedQuantity,
    decimal ValuedValue);
