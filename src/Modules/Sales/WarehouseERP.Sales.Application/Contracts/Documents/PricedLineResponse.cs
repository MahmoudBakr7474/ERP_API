using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Documents;

/// <summary>A quotation or invoice line as priced (a snapshot that later price list changes never touch).</summary>
/// <param name="ListUnitPrice">The price list's price when the line was entered; null when the list had none.</param>
/// <param name="NetUnitPrice">Unit price after discount.</param>
public sealed record PricedLineResponse(
    Guid Id, int LineNumber, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode,
    decimal ConversionFactor, decimal Quantity, decimal UnitPrice, PriceSource PriceSource, decimal? ListUnitPrice,
    decimal DiscountPercent, SalesTaxTreatment TaxTreatment, decimal TaxRate, decimal GrossAmount, decimal DiscountAmount,
    decimal NetAmount, decimal TaxAmount, decimal TotalAmount, decimal NetUnitPrice);
