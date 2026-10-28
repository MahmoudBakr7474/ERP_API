using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// One order line billed by an invoice: the supplier's price, discount and tax with their rounded amounts, the order's
/// net unit price it is matched to and the resulting purchase price variance.
/// </summary>
public sealed class SupplierInvoiceLine
{
    private SupplierInvoiceLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }

    /// <summary>In the order line's unit.</summary>
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

    /// <summary>The supplier's unit price after discount.</summary>
    public decimal NetUnitPrice { get; private set; }

    /// <summary>The order's net unit price, which goods entered stock at.</summary>
    public decimal OrderNetUnitPrice { get; private set; }

    /// <summary>(NetUnitPrice - OrderNetUnitPrice) x Quantity, rounded: positive when the supplier bills more than ordered.</summary>
    public decimal PriceVarianceAmount { get; private set; }

    public bool HasPriceVariance => NetUnitPrice != OrderNetUnitPrice;

    internal static Result<SupplierInvoiceLine> Create(int lineNumber, PurchaseOrderLine orderLine, InvoiceLineInput input, int currencyDecimals)
    {
        if (input.Quantity <= 0 || input.Quantity > Amounts.MaxQuantity || !Amounts.FitsScale(input.Quantity, Amounts.QuantityScale))
            return PurchaseLineDomainErrors.QuantityInvalid(lineNumber);
        if (input.Quantity > orderLine.InvoiceableQuantity)
            return PurchaseOrderDomainErrors.InvoiceExceedsReceipts(lineNumber, orderLine.InvoiceableQuantity);
        var amounts = LineAmounts.Calculate(input.Quantity, input.Price, currencyDecimals, lineNumber);
        if (amounts.IsFailure)
            return amounts.Error;

        return new SupplierInvoiceLine
        {
            LineNumber = lineNumber,
            PurchaseOrderLineId = orderLine.Id,
            ProductId = orderLine.ProductId,
            ProductUnitId = orderLine.ProductUnitId,
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
            OrderNetUnitPrice = orderLine.NetUnitPrice,
            PriceVarianceAmount = Amounts.Round((amounts.Value.NetUnitPrice - orderLine.NetUnitPrice) * input.Quantity, currencyDecimals),
        };
    }
}
