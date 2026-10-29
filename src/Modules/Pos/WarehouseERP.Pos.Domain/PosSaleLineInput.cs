using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>A cart line as Sales priced it, with Catalog's names for the receipt.</summary>
/// <param name="ConversionFactor">Base units in one of this unit, as Catalog had it.</param>
/// <param name="UnitPrice">The list price, tax-exclusive.</param>
/// <param name="NetUnitPrice">The unit price after discount, 8 decimals.</param>
public sealed record PosSaleLineInput(
    Guid ProductId, Guid ProductUnitId, Guid UnitId, decimal ConversionFactor, string Sku, string ProductName, string UnitCode,
    decimal Quantity, decimal UnitPrice, decimal DiscountPercent, PosTaxTreatment TaxTreatment, decimal TaxRate,
    decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal TaxAmount, decimal TotalAmount, decimal NetUnitPrice);
