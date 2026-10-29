using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// A returned quantity of one sale line, with the line's product, receipt names, price, discount and VAT as sold, the share
/// of its net and VAT refunded, and the PosReturn stock line Inventory posted with its cost. Never changes.
/// </summary>
public sealed class PosReturnLine
{
    private PosReturnLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ReturnId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid SaleLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public string Sku { get; private set; } = null!;
    public string ProductName { get; private set; } = null!;
    public string UnitCode { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public PosReturnCondition Condition { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public PosTaxTreatment TaxTreatment { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    /// <summary>The sale line's stock line the goods come back against.</summary>
    public Guid SoldStockLineId { get; private set; }

    /// <summary>The PosReturn stock document line.</summary>
    public Guid? StockLineId { get; private set; }

    public decimal? CostAmount { get; private set; }
    public decimal BaseQuantity => Quantity * ConversionFactor;

    internal static PosReturnLine Create(int lineNumber, PosReturnLineInput input)
    {
        var sold = input.Share.Line;
        return new PosReturnLine
        {
            LineNumber = lineNumber,
            SaleLineId = sold.Id,
            ProductId = sold.ProductId,
            ProductUnitId = sold.ProductUnitId,
            UnitId = sold.UnitId,
            ConversionFactor = sold.ConversionFactor,
            Sku = sold.Sku,
            ProductName = sold.ProductName,
            UnitCode = sold.UnitCode,
            Quantity = input.Share.Quantity,
            Condition = input.Condition,
            UnitPrice = sold.UnitPrice,
            DiscountPercent = sold.DiscountPercent,
            TaxTreatment = sold.TaxTreatment,
            TaxRate = sold.TaxRate,
            NetAmount = input.Share.NetAmount,
            TaxAmount = input.Share.TaxAmount,
            TotalAmount = input.Share.TotalAmount,
            SoldStockLineId = sold.StockLineId ?? throw new InvalidOperationException($"Sale line {sold.Id} has no stock line."),
        };
    }

    internal void LinkStock(Guid stockLineId, decimal costAmount)
    {
        StockLineId = stockLineId;
        CostAmount = costAmount;
    }
}
