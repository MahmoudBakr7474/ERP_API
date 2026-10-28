namespace WarehouseERP.Purchasing.Domain;

/// <summary>One order line's goods in a receipt, in the order line's unit, and how much of them went back to the supplier.</summary>
public sealed class GoodsReceiptLine
{
    private GoodsReceiptLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ReceiptId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }

    /// <summary>The order's net unit price, which Inventory took as the provisional cost of one unit.</summary>
    public decimal UnitCost { get; private set; }

    public Guid LocationId { get; private set; }

    /// <summary>What posted supplier returns sent back of this line.</summary>
    public decimal ReturnedQuantity { get; private set; }

    public decimal ReturnableQuantity => Quantity - ReturnedQuantity;

    internal static GoodsReceiptLine Create(int lineNumber, PurchaseOrderLine orderLine, decimal quantity, Guid locationId) =>
        new()
        {
            LineNumber = lineNumber,
            PurchaseOrderLineId = orderLine.Id,
            ProductId = orderLine.ProductId,
            ProductUnitId = orderLine.ProductUnitId,
            Quantity = quantity,
            UnitCost = orderLine.NetUnitPrice,
            LocationId = locationId,
        };

    internal void AddReturned(decimal quantity)
    {
        if (quantity > ReturnableQuantity)
            throw new InvalidOperationException($"Receipt line {Id} has only {ReturnableQuantity} left to return.");
        ReturnedQuantity += quantity;
    }
}
