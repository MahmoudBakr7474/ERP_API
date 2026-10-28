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

    /// <summary>On a reversal, the posted line it undoes.</summary>
    public Guid? ReversesLineId { get; private set; }

    public IReadOnlyList<StockAllocation> Allocations => _allocations;

    internal static Result<StockDocumentLine> Create(StockDocumentType documentType, Guid warehouseId, int lineNumber, StockLineInput input)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.Quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.ConversionFactor);

        var allowed = documentType switch
        {
            StockDocumentType.OpeningBalance or StockDocumentType.Receipt => input.Direction == StockDirection.In,
            StockDocumentType.Issue => input.Direction == StockDirection.Out,
            StockDocumentType.Adjustment => input.Direction is StockDirection.In or StockDirection.Out,
            _ => false,
        };
        if (!allowed)
            return StockDocumentDomainErrors.WrongDirection(lineNumber);
        if ((documentType is StockDocumentType.OpeningBalance or StockDocumentType.Receipt) && input.UnitCost is null)
            return StockDocumentDomainErrors.UnitCostRequired(lineNumber);
        if (input.Direction == StockDirection.Out && input.UnitCost is not null)
            return StockDocumentDomainErrors.UnitCostNotAllowed(lineNumber);
        if (input.UnitCost is { } unitCost
            && (unitCost / input.ConversionFactor > Quantities.MaxUnitCost || input.Quantity * unitCost > Quantities.MaxLineValue))
            return StockDocumentDomainErrors.UnitCostTooLarge(lineNumber);
        if (!input.UnitAllowsFraction && !Quantities.IsWhole(input.Quantity))
            return StockDocumentDomainErrors.QuantityMustBeWhole(lineNumber);

        var baseQuantity = input.Quantity * input.ConversionFactor;
        if (baseQuantity > Quantities.MaxQuantity)
            return StockDocumentDomainErrors.QuantityTooLarge(lineNumber);
        if (!Quantities.FitsScale(baseQuantity, Quantities.QuantityScale)
            || (!input.BaseUnitAllowsFraction && !Quantities.IsWhole(baseQuantity)))
            return StockDocumentDomainErrors.ConversionNotExact(lineNumber);
        baseQuantity = decimal.Round(baseQuantity, Quantities.QuantityScale);

        var line = new StockDocumentLine
        {
            LineNumber = lineNumber,
            ProductId = input.ProductId,
            ProductUnitId = input.ProductUnitId,
            UnitId = input.UnitId,
            ConversionFactor = input.ConversionFactor,
            Quantity = input.Quantity,
            BaseQuantity = baseQuantity,
            Direction = input.Direction,
            UnitCost = input.UnitCost,
        };
        line._allocations.Add(input.Direction == StockDirection.In
            ? StockAllocation.Into(warehouseId, input.LocationId, baseQuantity)
            : StockAllocation.OutOf(warehouseId, input.LocationId, baseQuantity));
        return line;
    }

    /// <summary>The same product and quantity the other way round, for a reversal.</summary>
    internal StockDocumentLine Reversed()
    {
        var line = new StockDocumentLine
        {
            LineNumber = LineNumber,
            ProductId = ProductId,
            ProductUnitId = ProductUnitId,
            UnitId = UnitId,
            ConversionFactor = ConversionFactor,
            Quantity = Quantity,
            BaseQuantity = BaseQuantity,
            Direction = Direction == StockDirection.In ? StockDirection.Out : StockDirection.In,
            ReversesLineId = Id,
        };
        foreach (var allocation in _allocations)
            line._allocations.Add(allocation.Reversed());
        return line;
    }
}
