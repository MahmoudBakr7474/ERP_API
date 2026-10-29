using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Features.Sales.QuotePosCart;

/// <param name="AvailableBaseQuantity">Unreserved stock of the product in the selling location, base units.</param>
/// <param name="StockShort">True when the cart's lines of this product need more than that.</param>
public sealed record PosCartQuoteLineResponse(
    int LineNumber, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode, decimal Quantity,
    decimal UnitPrice, decimal UnitPriceWithTax, decimal DiscountPercent, decimal GrossAmount, decimal DiscountAmount,
    decimal NetAmount, PosTaxTreatment TaxTreatment, decimal TaxRate, decimal TaxAmount, decimal TotalAmount,
    decimal AvailableBaseQuantity, bool StockShort);
