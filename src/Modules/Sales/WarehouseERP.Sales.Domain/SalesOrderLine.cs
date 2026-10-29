using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// One product of a sales order, priced as agreed (a snapshot), and what has happened since: delivered, closed (reduced
/// or closed with the order, its reservation released), returned and invoiced. All quantities are in the line's unit; the
/// reservation Inventory holds for the line is its base quantity minus what was delivered or closed.
/// </summary>
public sealed class SalesOrderLine : PricedLine
{
    private SalesOrderLine() { } // EF Core

    public Guid OrderId { get; private set; }
    public decimal DeliveredQuantity { get; private set; }

    /// <summary>Taken off the order after confirmation (reduced or closed); never delivered.</summary>
    public decimal ClosedQuantity { get; private set; }

    /// <summary>Everything the customer sent back, invoiced or not.</summary>
    public decimal ReturnedQuantity { get; private set; }

    /// <summary>The part of <see cref="ReturnedQuantity"/> that had been invoiced: a credit is owed to the customer.</summary>
    public decimal ReturnedInvoicedQuantity { get; private set; }

    public decimal InvoicedQuantity { get; private set; }

    /// <summary>Net amount of the posted invoices for this line, which values credits for returns.</summary>
    public decimal InvoicedNetAmount { get; private set; }

    /// <summary>Tax of the posted invoices for this line, which values credits' tax.</summary>
    public decimal InvoicedTaxAmount { get; private set; }

    /// <summary>What is still to be delivered (and still reserved) while the order is open.</summary>
    public decimal RemainingQuantity => Quantity - DeliveredQuantity - ClosedQuantity;

    /// <summary>Delivered, not returned before invoicing, and not invoiced yet: what an invoice may still bill.</summary>
    public decimal InvoiceableQuantity => DeliveredQuantity - (ReturnedQuantity - ReturnedInvoicedQuantity) - InvoicedQuantity;

    /// <summary>Delivered and not returned yet.</summary>
    public decimal ReturnableQuantity => DeliveredQuantity - ReturnedQuantity;

    internal static Result<SalesOrderLine> Create(int lineNumber, SalesLineInput input, int currencyDecimals)
    {
        var line = new SalesOrderLine();
        var filled = line.Fill(lineNumber, input, currencyDecimals);
        return filled.IsFailure ? filled.Error : line;
    }

    internal void AddDelivered(decimal quantity) => DeliveredQuantity += quantity;

    internal void AddClosed(decimal quantity) => ClosedQuantity += quantity;

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
        var creditNet = invoiced == 0 ? 0m : Amounts.Round(invoiced * InvoicedNetAmount / InvoicedQuantity, currencyDecimals);
        var creditTax = invoiced == 0 ? 0m : Amounts.Round(invoiced * InvoicedTaxAmount / InvoicedQuantity, currencyDecimals);
        ReturnedQuantity += quantity;
        ReturnedInvoicedQuantity += invoiced;
        return new ReturnSplit(uninvoiced, invoiced, creditNet, creditTax);
    }
}
