namespace WarehouseERP.Sales.Presentation.Contracts.Prices;

/// <summary>The product unit's selling price, tax-exclusive, in the base currency.</summary>
public sealed record SetSalesPriceRequest(decimal UnitPrice);
