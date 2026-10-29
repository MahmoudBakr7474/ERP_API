using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Items;

/// <summary>
/// A product unit as a till sees it: names, the list price (tax-exclusive) and the price with VAT, its tax treatment, and
/// stock nobody reserved in the terminal's selling location (a hint; checkout checks again). When it can't be sold,
/// <see cref="ProblemCode"/> says why (for example Sales.Line.PriceMissing or Sales.ProductTax.NotClassified).
/// </summary>
/// <param name="AvailableBaseQuantity">Sellable stock in base units.</param>
/// <param name="AvailableQuantity">The same in this unit.</param>
public sealed record PosItemResponse(
    Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, Guid UnitId, string UnitCode, string UnitName,
    decimal ConversionFactor, bool IsBaseUnit, bool UnitAllowsFraction, string CurrencyCode, decimal? UnitPrice,
    decimal? UnitPriceWithTax, PosTaxTreatment? TaxTreatment, decimal TaxRate, decimal AvailableBaseQuantity,
    decimal AvailableQuantity, bool CanSell, string? ProblemCode);
