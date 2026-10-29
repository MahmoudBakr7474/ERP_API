namespace WarehouseERP.Sales.Application.Features.Prices.SetSalesPrice;

/// <summary>Sets the selling price of a product unit, tax-exclusive, in the base currency.</summary>
/// <param name="UnitPrice">Above 0, at most 8 decimals.</param>
public sealed record SetSalesPriceCommand(Guid ProductUnitId, decimal UnitPrice);
