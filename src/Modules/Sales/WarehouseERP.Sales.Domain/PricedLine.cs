using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// What every priced sales line keeps: its product unit as Catalog had it, the agreed price as a snapshot (with the list
/// price of the moment and where the price came from), the discount and VAT, and the rounded amounts. A later price list
/// change never touches it. Quotation and order lines each keep these in their own table.
/// </summary>
public abstract class PricedLine
{
    public Guid Id { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }

    /// <summary>Base units in one unit of the line, kept as Catalog had it when the line was entered.</summary>
    public decimal ConversionFactor { get; private set; }

    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public PriceSource PriceSource { get; private set; }

    /// <summary>The price list's price when the line was entered; null when the list had none.</summary>
    public decimal? ListUnitPrice { get; private set; }

    public decimal DiscountPercent { get; private set; }
    public SalesTaxTreatment TaxTreatment { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    /// <summary>Unit price after discount, 8 decimals.</summary>
    public decimal NetUnitPrice { get; private set; }

    /// <summary>The line's base quantity: exactly Quantity times ConversionFactor.</summary>
    public decimal BaseQuantity => Quantity * ConversionFactor;

    /// <summary>The agreed price, discount and tax, as another document copies them.</summary>
    public LinePrice Price => new(UnitPrice, DiscountPercent, TaxTreatment, TaxRate);

    protected Result Fill(int lineNumber, SalesLineInput input, int currencyDecimals)
    {
        var quantity = input.Unit.CheckQuantity(input.Quantity, lineNumber);
        if (quantity.IsFailure)
            return quantity.Error;
        var amounts = LineAmounts.Calculate(input.Quantity, input.Price, currencyDecimals, lineNumber);
        if (amounts.IsFailure)
            return amounts.Error;

        LineNumber = lineNumber;
        ProductId = input.Unit.ProductId;
        ProductUnitId = input.Unit.ProductUnitId;
        UnitId = input.Unit.UnitId;
        ConversionFactor = input.Unit.ConversionFactor;
        Quantity = input.Quantity;
        UnitPrice = input.Price.UnitPrice;
        PriceSource = input.PriceSource;
        ListUnitPrice = input.ListUnitPrice;
        DiscountPercent = input.Price.DiscountPercent;
        TaxTreatment = input.Price.TaxTreatment;
        TaxRate = input.Price.TaxRate;
        GrossAmount = amounts.Value.Gross;
        DiscountAmount = amounts.Value.Discount;
        NetAmount = amounts.Value.Net;
        TaxAmount = amounts.Value.Tax;
        TotalAmount = amounts.Value.Total;
        NetUnitPrice = amounts.Value.NetUnitPrice;
        return Result.Success();
    }

    /// <summary>The line as input again, for a document that copies it (a quotation becoming an order).</summary>
    public SalesLineInput ToInput(ProductUnitFacts unit) => new(unit, Quantity, Price, PriceSource, ListUnitPrice);
}
