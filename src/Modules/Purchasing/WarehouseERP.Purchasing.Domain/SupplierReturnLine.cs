using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// One receipt line's goods going back, in the order line's unit, from a location of the warehouse. Once posted it keeps
/// how the quantity divided between goods not yet invoiced and goods already invoiced, and the credit claimed for the latter.
/// </summary>
public sealed class SupplierReturnLine
{
    private SupplierReturnLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ReturnId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid GoodsReceiptLineId { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid LocationId { get; private set; }

    /// <summary>The provisional cost the goods entered stock at; they leave at Inventory's current average.</summary>
    public decimal ReceiptUnitCost { get; private set; }

    /// <summary>Returned before invoicing (set when posted): it will not be invoiced.</summary>
    public decimal UninvoicedQuantity { get; private set; }

    /// <summary>Returned after invoicing (set when posted): the supplier owes a credit for it.</summary>
    public decimal InvoicedQuantity { get; private set; }

    public decimal CreditClaimNetAmount { get; private set; }
    public decimal CreditClaimTaxAmount { get; private set; }

    internal static Result<SupplierReturnLine> Create(int lineNumber, GoodsReceiptLine receiptLine, ReturnLineInput input)
    {
        if (input.Quantity <= 0 || !Amounts.FitsScale(input.Quantity, Amounts.QuantityScale))
            return PurchaseLineDomainErrors.QuantityInvalid(lineNumber);
        if (input.Quantity > receiptLine.ReturnableQuantity)
            return SupplierReturnDomainErrors.ExceedsReceipt(lineNumber, receiptLine.ReturnableQuantity);

        return new SupplierReturnLine
        {
            LineNumber = lineNumber,
            GoodsReceiptLineId = receiptLine.Id,
            PurchaseOrderLineId = receiptLine.PurchaseOrderLineId,
            ProductId = receiptLine.ProductId,
            ProductUnitId = receiptLine.ProductUnitId,
            Quantity = input.Quantity,
            LocationId = input.LocationId ?? receiptLine.LocationId,
            ReceiptUnitCost = receiptLine.UnitCost,
        };
    }

    internal void ApplySplit(ReturnSplit split)
    {
        if (split.UninvoicedQuantity + split.InvoicedQuantity != Quantity)
            throw new ArgumentException("A split divides the line's quantity.", nameof(split));
        UninvoicedQuantity = split.UninvoicedQuantity;
        InvoicedQuantity = split.InvoicedQuantity;
        CreditClaimNetAmount = split.CreditClaimNet;
        CreditClaimTaxAmount = split.CreditClaimTax;
    }
}
