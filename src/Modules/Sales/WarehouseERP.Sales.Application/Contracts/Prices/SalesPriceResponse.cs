namespace WarehouseERP.Sales.Application.Contracts.Prices;

/// <summary>A price list entry: the selling price of one product unit, tax-exclusive, in the base currency.</summary>
/// <param name="ConversionFactor">Base units in one of this unit.</param>
public sealed record SalesPriceResponse(
    Guid Id, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode, decimal ConversionFactor,
    decimal UnitPrice, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
