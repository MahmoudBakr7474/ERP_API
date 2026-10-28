using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>One product a request asks for, in a product unit, and how much of it is on order.</summary>
public sealed class PurchaseRequestLine
{
    private PurchaseRequestLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductUnitId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal ConversionFactor { get; private set; }
    public decimal Quantity { get; private set; }

    /// <summary>What orders that are not cancelled have taken from this line, in its unit.</summary>
    public decimal OrderedQuantity { get; private set; }

    public string? Notes { get; private set; }

    public decimal RemainingQuantity => Quantity - OrderedQuantity;

    internal static Result<PurchaseRequestLine> Create(int lineNumber, RequestLineInput input)
    {
        var quantity = input.Unit.CheckQuantity(input.Quantity, lineNumber);
        if (quantity.IsFailure)
            return quantity.Error;
        return new PurchaseRequestLine
        {
            LineNumber = lineNumber,
            ProductId = input.Unit.ProductId,
            ProductUnitId = input.Unit.ProductUnitId,
            UnitId = input.Unit.UnitId,
            ConversionFactor = input.Unit.ConversionFactor,
            Quantity = input.Quantity,
            Notes = Text.Optional(input.Notes),
        };
    }

    internal void AddOrdered(decimal quantity) => OrderedQuantity += quantity;
}
