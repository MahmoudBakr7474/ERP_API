using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Orders;

/// <param name="ListUnitPrice">The price list's price when the line was entered; null when the list had none.</param>
/// <param name="ClosedQuantity">Taken off after confirmation (reduced, or closed with the order); its reservation was released.</param>
/// <param name="RemainingQuantity">Still to be delivered, and still reserved, while the order is open.</param>
/// <param name="InvoiceableQuantity">Delivered, not returned before invoicing, and not invoiced yet.</param>
/// <param name="ReturnedInvoicedQuantity">Returned after invoicing: a credit is owed to the customer for it.</param>
public sealed record SalesOrderLineResponse(
    Guid Id, int LineNumber, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode,
    decimal ConversionFactor, decimal Quantity, decimal UnitPrice, PriceSource PriceSource, decimal? ListUnitPrice,
    decimal DiscountPercent, SalesTaxTreatment TaxTreatment, decimal TaxRate, decimal GrossAmount, decimal DiscountAmount,
    decimal NetAmount, decimal TaxAmount, decimal TotalAmount, decimal NetUnitPrice, decimal DeliveredQuantity,
    decimal ClosedQuantity, decimal RemainingQuantity, decimal InvoicedQuantity, decimal InvoiceableQuantity,
    decimal ReturnedQuantity, decimal ReturnedInvoicedQuantity);
