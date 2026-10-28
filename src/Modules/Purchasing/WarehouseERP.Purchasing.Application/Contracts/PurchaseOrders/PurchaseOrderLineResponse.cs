using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;

/// <param name="NetUnitPrice">Unit price after discount: the provisional inventory cost and the price invoices are matched to.</param>
/// <param name="RemainingQuantity">What may still be received; 0 once the order is closed or cancelled.</param>
/// <param name="InvoiceableQuantity">Received, not returned before invoicing, and not invoiced yet.</param>
/// <param name="ReturnedInvoicedQuantity">Returned after invoicing: the supplier owes a credit for it.</param>
public sealed record PurchaseOrderLineResponse(
    Guid Id, int LineNumber, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode,
    decimal ConversionFactor, decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxRate,
    TaxTreatment TaxTreatment, decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal TaxAmount,
    decimal TotalAmount, decimal NetUnitPrice, Guid? PurchaseRequestLineId, decimal ReceivedQuantity, decimal RemainingQuantity,
    decimal InvoicedQuantity, decimal InvoiceableQuantity, decimal ReturnedQuantity, decimal ReturnedInvoicedQuantity);
