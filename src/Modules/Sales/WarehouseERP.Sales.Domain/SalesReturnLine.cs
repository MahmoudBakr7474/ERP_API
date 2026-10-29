using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// One returned quantity of a delivery line, fit for resale, into a location of the delivery's warehouse. Approval
/// records the stock line it came back on, its cost (the delivery line's), and how it split between goods never invoiced
/// and invoiced goods, which the company owes a credit for.
/// </summary>
public sealed class SalesReturnLine
{
    private SalesReturnLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ReturnId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid DeliveryLineId { get; private set; }
    public Guid OrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public decimal Quantity { get; private set; }
    public ReturnCondition Condition { get; private set; }
    public Guid LocationId { get; private set; }

    /// <summary>The SalesReturn stock document line Inventory posted.</summary>
    public Guid? StockLineId { get; private set; }

    /// <summary>The value it came back at: its delivery line's cost, in proportion.</summary>
    public decimal? CostAmount { get; private set; }

    public decimal UninvoicedQuantity { get; private set; }
    public decimal InvoicedQuantity { get; private set; }
    public decimal CreditNetAmount { get; private set; }
    public decimal CreditTaxAmount { get; private set; }

    internal static SalesReturnLine Create(int lineNumber, ReturnLineInput input, SalesDeliveryLine deliveryLine) => new()
    {
        LineNumber = lineNumber,
        DeliveryLineId = deliveryLine.Id,
        OrderLineId = deliveryLine.OrderLineId,
        ProductId = deliveryLine.ProductId,
        ProductUnitId = deliveryLine.ProductUnitId,
        UnitId = deliveryLine.UnitId,
        ConversionFactor = deliveryLine.ConversionFactor,
        Quantity = input.Quantity,
        Condition = input.Condition,
        LocationId = input.LocationId,
    };

    internal void Settle(Guid stockLineId, decimal costAmount, ReturnSplit split)
    {
        StockLineId = stockLineId;
        CostAmount = costAmount;
        UninvoicedQuantity = split.UninvoicedQuantity;
        InvoicedQuantity = split.InvoicedQuantity;
        CreditNetAmount = split.CreditNetAmount;
        CreditTaxAmount = split.CreditTaxAmount;
    }
}
