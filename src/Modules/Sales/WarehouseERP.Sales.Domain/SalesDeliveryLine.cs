namespace WarehouseERP.Sales.Domain;

/// <summary>
/// One picked quantity of a delivery: an order line's product in the order line's unit, from one location. Once posted
/// it names the stock document line it left on and what it cost; returns count against it.
/// </summary>
public sealed class SalesDeliveryLine
{
    private SalesDeliveryLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid DeliveryId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid OrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid LocationId { get; private set; }

    /// <summary>The stock document line Inventory posted; a return comes back from it.</summary>
    public Guid? StockLineId { get; private set; }

    /// <summary>What the goods left stock at (moving average), in the base currency; the cost of goods sold.</summary>
    public decimal? CostAmount { get; private set; }

    /// <summary>What customer returns brought back from this line.</summary>
    public decimal ReturnedQuantity { get; private set; }

    public decimal BaseQuantity => Quantity * ConversionFactor;

    /// <summary>Delivered and not returned yet.</summary>
    public decimal ReturnableQuantity => Quantity - ReturnedQuantity;

    internal static SalesDeliveryLine Create(int lineNumber, DeliveryLineInput input, SalesOrderLine orderLine) => new()
    {
        LineNumber = lineNumber,
        OrderLineId = orderLine.Id,
        ProductId = orderLine.ProductId,
        ProductUnitId = orderLine.ProductUnitId,
        UnitId = orderLine.UnitId,
        ConversionFactor = orderLine.ConversionFactor,
        Quantity = input.Quantity,
        LocationId = input.LocationId,
    };

    internal void LinkStock(Guid stockLineId, decimal costAmount)
    {
        StockLineId = stockLineId;
        CostAmount = costAmount;
    }

    internal void AddReturned(decimal quantity) => ReturnedQuantity += quantity;
}
