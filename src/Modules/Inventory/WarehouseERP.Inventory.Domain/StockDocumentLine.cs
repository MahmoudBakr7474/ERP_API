using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// One product on a document: the unit and quantity as entered, the conversion factor Catalog had then, the exact base
/// quantity, and the allocations that place it in locations.
/// </summary>
public sealed class StockDocumentLine
{
    private readonly List<StockAllocation> _allocations = [];

    private StockDocumentLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }

    /// <summary>Base units in one entered unit, kept as Catalog had it when the line was entered.</summary>
    public decimal ConversionFactor { get; private set; }

    /// <summary>Quantity in the entered unit.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Quantity in the product's base unit: exactly Quantity times ConversionFactor.</summary>
    public decimal BaseQuantity { get; private set; }

    public StockDirection Direction { get; private set; }

    /// <summary>Cost of one entered unit in the base currency; only stock coming in carries one.</summary>
    public decimal? UnitCost { get; private set; }

    /// <summary>
    /// On a reversal, the posted line it undoes; on a sales return, the delivery line the goods come back from; on a POS return,
    /// the sale line.
    /// </summary>
    public Guid? ReversesLineId { get; private set; }

    public IReadOnlyList<StockAllocation> Allocations => _allocations;

    internal static Result<StockDocumentLine> Create(StockDocumentType documentType, Guid warehouseId, int lineNumber, StockLineInput input)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.Quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.ConversionFactor);

        var allowed = documentType switch
        {
            StockDocumentType.OpeningBalance or StockDocumentType.Receipt or StockDocumentType.PurchaseReceipt =>
                input.Direction == StockDirection.In,
            StockDocumentType.SalesReturn or StockDocumentType.PosReturn => input.Direction == StockDirection.In,
            StockDocumentType.Issue or StockDocumentType.PurchaseReturn or StockDocumentType.SalesDelivery or StockDocumentType.PosSale =>
                input.Direction == StockDirection.Out,
            StockDocumentType.Adjustment => input.Direction is StockDirection.In or StockDirection.Out,
            _ => false,
        };
        if (!allowed)
            return StockDocumentDomainErrors.WrongDirection(lineNumber);
        if (documentType is StockDocumentType.OpeningBalance or StockDocumentType.Receipt or StockDocumentType.PurchaseReceipt
            && input.UnitCost is null)
            return StockDocumentDomainErrors.UnitCostRequired(lineNumber);
        if (input.Direction == StockDirection.Out && input.UnitCost is not null)
            return StockDocumentDomainErrors.UnitCostNotAllowed(lineNumber);
        if (input.UnitCost is { } unitCost
            && (unitCost / input.ConversionFactor > Quantities.MaxUnitCost || input.Quantity * unitCost > Quantities.MaxLineValue))
            return StockDocumentDomainErrors.UnitCostTooLarge(lineNumber);
        var baseQuantity = Quantities.ToBase(
            input.Quantity, input.ConversionFactor, input.UnitAllowsFraction, input.BaseUnitAllowsFraction, lineNumber);
        if (baseQuantity.IsFailure)
            return baseQuantity.Error;
        if ((documentType is StockDocumentType.SalesReturn or StockDocumentType.PosReturn) != input.ReturnsLineId.HasValue)
            throw new ArgumentException("A sales or POS return line, and only one, names the line it returns.", nameof(input));

        var line = New(lineNumber, input.ProductId, input.ProductUnitId, input.UnitId, input.ConversionFactor, input.Quantity,
            baseQuantity.Value, input.Direction, input.UnitCost);
        line.ReversesLineId = input.ReturnsLineId;
        line._allocations.Add(input.Direction == StockDirection.In
            ? StockAllocation.Into(warehouseId, input.LocationId, baseQuantity.Value)
            : StockAllocation.OutOf(warehouseId, input.LocationId, baseQuantity.Value));
        return line;
    }

    /// <summary>A transfer line: the quantity as the transfer has it, moved from one place to another, without cost.</summary>
    internal static StockDocumentLine CreateMove(
        int lineNumber, Guid productId, Guid productUnitId, Guid unitId, decimal conversionFactor, decimal quantity,
        decimal baseQuantity, StockPlace from, StockPlace to)
    {
        var line = New(lineNumber, productId, productUnitId, unitId, conversionFactor, quantity, baseQuantity, StockDirection.Move, null);
        line._allocations.Add(StockAllocation.Move(from, to, baseQuantity));
        return line;
    }

    /// <summary>A line a transfer or a count produces, in base units at one place.</summary>
    internal static StockDocumentLine CreateGenerated(int lineNumber, GeneratedLineInput input)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.BaseQuantity);
        if (input.Direction is not (StockDirection.In or StockDirection.Out))
            throw new ArgumentException("A generated line adds or removes stock.", nameof(input));

        var line = New(lineNumber, input.ProductId, input.ProductUnitId, input.UnitId, 1m, input.BaseQuantity, input.BaseQuantity,
            input.Direction, input.Direction == StockDirection.In ? input.UnitCost : null);
        line._allocations.Add(input.Direction == StockDirection.In
            ? StockAllocation.Into(input.Place.WarehouseId, input.Place.LocationId, input.BaseQuantity)
            : StockAllocation.OutOf(input.Place.WarehouseId, input.Place.LocationId, input.BaseQuantity));
        return line;
    }

    /// <summary>The same product and quantity the other way round, for a reversal.</summary>
    internal StockDocumentLine Reversed()
    {
        var line = New(LineNumber, ProductId, ProductUnitId, UnitId, ConversionFactor, Quantity, BaseQuantity,
            Direction == StockDirection.In ? StockDirection.Out : StockDirection.In, null);
        line.ReversesLineId = Id;
        foreach (var allocation in _allocations)
            line._allocations.Add(allocation.Reversed());
        return line;
    }

    private static StockDocumentLine New(
        int lineNumber, Guid productId, Guid productUnitId, Guid unitId, decimal conversionFactor, decimal quantity,
        decimal baseQuantity, StockDirection direction, decimal? unitCost) =>
        new()
        {
            LineNumber = lineNumber,
            ProductId = productId,
            ProductUnitId = productUnitId,
            UnitId = unitId,
            ConversionFactor = conversionFactor,
            Quantity = quantity,
            BaseQuantity = baseQuantity,
            Direction = direction,
            UnitCost = unitCost,
        };
}
