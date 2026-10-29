using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// A sold line: the product unit with its receipt names, the quantity, the price, discount and VAT Sales gave it with the
/// rounded amounts, the stock line Inventory posted and its cost, and what came back from it (quantity and the refunded
/// net and VAT, cumulative, so partial returns always add up to the line).
/// </summary>
public sealed class PosSaleLine
{
    private PosSaleLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public string Sku { get; private set; } = null!;
    public string ProductName { get; private set; } = null!;
    public string UnitCode { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public PosTaxTreatment TaxTreatment { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal NetUnitPrice { get; private set; }

    /// <summary>The PosSale stock document line; returns come back against it.</summary>
    public Guid? StockLineId { get; private set; }

    /// <summary>What the goods cost at the moving average when they left.</summary>
    public decimal? CostAmount { get; private set; }

    public decimal ReturnedQuantity { get; private set; }
    public decimal RefundedNetAmount { get; private set; }
    public decimal RefundedTaxAmount { get; private set; }
    public decimal BaseQuantity => Quantity * ConversionFactor;
    public decimal ReturnableQuantity => Quantity - ReturnedQuantity;

    internal static PosSaleLine Create(int lineNumber, PosSaleLineInput input) => new()
    {
        LineNumber = lineNumber,
        ProductId = input.ProductId,
        ProductUnitId = input.ProductUnitId,
        UnitId = input.UnitId,
        ConversionFactor = input.ConversionFactor,
        Sku = input.Sku,
        ProductName = input.ProductName,
        UnitCode = input.UnitCode,
        Quantity = input.Quantity,
        UnitPrice = input.UnitPrice,
        DiscountPercent = input.DiscountPercent,
        TaxTreatment = input.TaxTreatment,
        TaxRate = input.TaxRate,
        GrossAmount = input.GrossAmount,
        DiscountAmount = input.DiscountAmount,
        NetAmount = input.NetAmount,
        TaxAmount = input.TaxAmount,
        TotalAmount = input.TotalAmount,
        NetUnitPrice = input.NetUnitPrice,
    };

    internal void LinkStock(Guid stockLineId, decimal costAmount)
    {
        StockLineId = stockLineId;
        CostAmount = costAmount;
    }

    // The quantity's share of the line: cumulative shares up to the units back so far, less what earlier returns took.
    internal ReturnShare TakeBack(decimal quantity, int currencyDecimals)
    {
        var returned = ReturnedQuantity + quantity;
        var net = Money.CumulativeShare(NetAmount, returned, Quantity, currencyDecimals) - RefundedNetAmount;
        var tax = Money.CumulativeShare(TaxAmount, returned, Quantity, currencyDecimals) - RefundedTaxAmount;
        ReturnedQuantity = returned;
        RefundedNetAmount += net;
        RefundedTaxAmount += tax;
        return new ReturnShare(this, quantity, net, tax);
    }
}
