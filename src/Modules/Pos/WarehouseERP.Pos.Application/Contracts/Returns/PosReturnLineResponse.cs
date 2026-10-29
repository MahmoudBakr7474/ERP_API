using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Returns;

/// <param name="UnitPrice">Tax-exclusive price of one unit, as sold.</param>
/// <param name="UnitPriceWithTax">The same with its VAT, rounded, for display.</param>
public sealed record PosReturnLineResponse(
    Guid Id, int LineNumber, Guid SaleLineId, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode,
    decimal Quantity, PosReturnCondition Condition, decimal UnitPrice, decimal UnitPriceWithTax, decimal DiscountPercent,
    PosTaxTreatment TaxTreatment,
    decimal TaxRate, decimal NetAmount, decimal TaxAmount, decimal TotalAmount);
