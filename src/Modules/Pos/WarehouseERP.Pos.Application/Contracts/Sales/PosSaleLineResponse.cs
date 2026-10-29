using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Sales;

/// <param name="UnitPrice">Tax-exclusive list price of one unit.</param>
/// <param name="UnitPriceWithTax">The same with its VAT, rounded, for display.</param>
/// <param name="ReturnableQuantity">What may still come back.</param>
public sealed record PosSaleLineResponse(
    Guid Id, int LineNumber, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode, decimal Quantity,
    decimal UnitPrice, decimal UnitPriceWithTax, decimal DiscountPercent, decimal GrossAmount, decimal DiscountAmount,
    decimal NetAmount, PosTaxTreatment TaxTreatment, decimal TaxRate, decimal TaxAmount, decimal TotalAmount,
    decimal ReturnedQuantity, decimal ReturnableQuantity);
