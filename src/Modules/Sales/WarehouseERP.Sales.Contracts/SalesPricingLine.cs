namespace WarehouseERP.Sales.Contracts;

/// <summary>A line to price.</summary>
/// <param name="ProductId">The product, whose tax classification applies.</param>
/// <param name="ProductUnitId">The product unit, whose list price applies.</param>
/// <param name="Quantity">Quantity in that unit, above 0 with at most 4 decimals.</param>
/// <param name="DiscountPercent">Line discount, 0 to 100 with at most 4 decimals.</param>
public sealed record SalesPricingLine(Guid ProductId, Guid ProductUnitId, decimal Quantity, decimal DiscountPercent);
