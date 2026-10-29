using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Invoices;

/// <summary>A billed quantity at the order line's agreed price, discount and VAT.</summary>
public sealed record SalesInvoiceLineResponse(
    Guid Id, int LineNumber, Guid OrderLineId, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode,
    decimal Quantity, decimal UnitPrice, decimal DiscountPercent, SalesTaxTreatment TaxTreatment, decimal TaxRate,
    decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal TaxAmount, decimal TotalAmount);
