using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// One product of an order: its unit as Catalog had it, the agreed price, discount and tax with their rounded amounts,
/// and what has happened since: received, invoiced and returned (returned after invoicing being a credit owed by the
/// supplier). All quantities are in the line's unit.
/// </summary>
public sealed class PurchaseOrderLine
{
    private PurchaseOrderLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }

    /// <summary>Base units in one unit of the line, kept as Catalog had it when the line was entered.</summary>
    public decimal ConversionFactor { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxRate { get; private set; }
    public TaxTreatment TaxTreatment { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    /// <summary>Unit price after discount: the provisional inventory cost of one unit and the price invoices are matched to.</summary>
    public decimal NetUnitPrice { get; private set; }

    /// <summary>The approved purchase request line this line orders, when it came from one.</summary>
    public Guid? PurchaseRequestLineId { get; private set; }

    public decimal ReceivedQuantity { get; private set; }

    /// <summary>Everything returned to the supplier, invoiced or not.</summary>
    public decimal ReturnedQuantity { get; private set; }

    /// <summary>The part of <see cref="ReturnedQuantity"/> that had already been invoiced: the supplier owes a credit for it.</summary>
    public decimal ReturnedInvoicedQuantity { get; private set; }

    public decimal InvoicedQuantity { get; private set; }

    /// <summary>Net amount of the posted invoices for this line, which values a credit claim.</summary>
    public decimal InvoicedNetAmount { get; private set; }

    /// <summary>Tax of the posted invoices for this line, which values a credit claim's tax.</summary>
    public decimal InvoicedTaxAmount { get; private set; }

    /// <summary>What may still be received while the order is open.</summary>
    public decimal RemainingQuantity => Quantity - ReceivedQuantity;

    /// <summary>Received, not returned before invoicing, and not invoiced yet: what an invoice may still bill.</summary>
    public decimal InvoiceableQuantity => ReceivedQuantity - (ReturnedQuantity - ReturnedInvoicedQuantity) - InvoicedQuantity;

    internal static Result<PurchaseOrderLine> Create(int lineNumber, OrderLineInput input, int currencyDecimals)
    {
        var quantity = input.Unit.CheckQuantity(input.Quantity, lineNumber);
        if (quantity.IsFailure)
            return quantity.Error;
        var amounts = LineAmounts.Calculate(input.Quantity, input.Price, currencyDecimals, lineNumber);
        if (amounts.IsFailure)
            return amounts.Error;

        return new PurchaseOrderLine
        {
            LineNumber = lineNumber,
            ProductId = input.Unit.ProductId,
            ProductUnitId = input.Unit.ProductUnitId,
            UnitId = input.Unit.UnitId,
            ConversionFactor = input.Unit.ConversionFactor,
            Quantity = input.Quantity,
            UnitPrice = input.Price.UnitPrice,
            DiscountPercent = input.Price.DiscountPercent,
            TaxRate = input.Price.TaxRate,
            TaxTreatment = input.Price.TaxTreatment,
            GrossAmount = amounts.Value.Gross,
            DiscountAmount = amounts.Value.Discount,
            NetAmount = amounts.Value.Net,
            TaxAmount = amounts.Value.Tax,
            TotalAmount = amounts.Value.Total,
            NetUnitPrice = amounts.Value.NetUnitPrice,
            PurchaseRequestLineId = input.PurchaseRequestLineId,
        };
    }

    internal void AddReceived(decimal quantity) => ReceivedQuantity += quantity;

    internal void AddInvoiced(decimal quantity, decimal netAmount, decimal taxAmount)
    {
        InvoicedQuantity += quantity;
        InvoicedNetAmount += netAmount;
        InvoicedTaxAmount += taxAmount;
    }

    internal ReturnSplit AddReturned(decimal quantity, int currencyDecimals)
    {
        var uninvoiced = Math.Min(quantity, InvoiceableQuantity);
        var invoiced = quantity - uninvoiced;
        var claimNet = invoiced == 0 ? 0m : Amounts.Round(invoiced * InvoicedNetAmount / InvoicedQuantity, currencyDecimals);
        var claimTax = invoiced == 0 ? 0m : Amounts.Round(invoiced * InvoicedTaxAmount / InvoicedQuantity, currencyDecimals);
        ReturnedQuantity += quantity;
        ReturnedInvoicedQuantity += invoiced;
        return new ReturnSplit(uninvoiced, invoiced, claimNet, claimTax);
    }
}
