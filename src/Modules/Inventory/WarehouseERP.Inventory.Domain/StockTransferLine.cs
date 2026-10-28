using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// One product of a transfer: the quantity as entered and in base units, where it is picked from, and how much of it
/// arrived or was written off. What is neither is still in transit.
/// </summary>
public sealed class StockTransferLine
{
    private StockTransferLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TransferId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal BaseQuantity { get; private set; }
    public Guid SourceWarehouseId { get; private set; }
    public Guid SourceLocationId { get; private set; }

    /// <summary>Base quantity that arrived in the destination warehouse.</summary>
    public decimal ReceivedQuantity { get; private set; }

    /// <summary>Base quantity written off by the approved loss adjustment that closed the transfer.</summary>
    public decimal LostQuantity { get; private set; }

    /// <summary>Base quantity still in the transfer's transit location (once dispatched).</summary>
    public decimal OutstandingQuantity => BaseQuantity - ReceivedQuantity - LostQuantity;

    internal static Result<StockTransferLine> Create(Guid sourceWarehouseId, int lineNumber, TransferLineInput input)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.Quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.ConversionFactor);
        var baseQuantity = Quantities.ToBase(
            input.Quantity, input.ConversionFactor, input.UnitAllowsFraction, input.BaseUnitAllowsFraction, lineNumber);
        if (baseQuantity.IsFailure)
            return baseQuantity.Error;

        return new StockTransferLine
        {
            LineNumber = lineNumber,
            ProductId = input.ProductId,
            ProductUnitId = input.ProductUnitId,
            UnitId = input.UnitId,
            ConversionFactor = input.ConversionFactor,
            Quantity = input.Quantity,
            BaseQuantity = baseQuantity.Value,
            SourceWarehouseId = sourceWarehouseId,
            SourceLocationId = input.SourceLocationId,
        };
    }

    internal void Receive(decimal quantity) => ReceivedQuantity += quantity;

    internal void WriteOffOutstanding() => LostQuantity += OutstandingQuantity;
}
