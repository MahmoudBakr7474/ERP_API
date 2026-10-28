using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

/// <param name="OrderNetUnitPrice">The order's net unit price the goods entered stock at.</param>
/// <param name="PriceVarianceAmount">(NetUnitPrice - OrderNetUnitPrice) x Quantity: kept for Accounting, inventory is not revalued.</param>
public sealed record SupplierInvoiceLineResponse(
    Guid Id, int LineNumber, Guid PurchaseOrderLineId, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId,
    string UnitCode, decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxRate, TaxTreatment TaxTreatment,
    decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal TaxAmount, decimal TotalAmount, decimal NetUnitPrice,
    decimal OrderNetUnitPrice, decimal PriceVarianceAmount);
