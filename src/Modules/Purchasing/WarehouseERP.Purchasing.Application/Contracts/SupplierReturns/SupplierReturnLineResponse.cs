namespace WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

/// <param name="ReceiptUnitCost">The provisional cost the goods entered stock at; they leave at Inventory's moving average.</param>
/// <param name="UninvoicedQuantity">Returned before invoicing (known once posted): it won't be invoiced.</param>
/// <param name="InvoicedQuantity">Returned after invoicing (known once posted): the supplier owes a credit for it.</param>
public sealed record SupplierReturnLineResponse(
    Guid Id, int LineNumber, Guid GoodsReceiptLineId, Guid PurchaseOrderLineId, Guid ProductId, string Sku, string ProductName,
    Guid ProductUnitId, string UnitCode, decimal Quantity, Guid LocationId, string LocationCode, decimal ReceiptUnitCost,
    decimal UninvoicedQuantity, decimal InvoicedQuantity, decimal CreditClaimNetAmount, decimal CreditClaimTaxAmount);
