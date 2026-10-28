using FluentValidation;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments;

// The shape of one line, shared by the create and update validators. Whether the product, unit and location may be
// used is checked against Catalog and Organization by StockReferences.
internal static class StockLineRules
{
    public static void Apply(InlineValidator<StockLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.ProductId).NotEmpty();
        line.RuleFor(x => x.ProductUnitId).NotEmpty();
        line.RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(Quantities.MaxQuantity)
            .Must(q => Quantities.FitsScale(q, Quantities.QuantityScale))
            .WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.LocationId).NotEqual(Guid.Empty).When(x => x.LocationId.HasValue);
        line.RuleFor(x => x.UnitCost!.Value)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(Quantities.MaxUnitCost)
            .Must(c => Quantities.FitsScale(c, Quantities.CostScale))
            .WithMessage("'Unit Cost' may have at most 8 decimals.")
            .OverridePropertyName(nameof(StockLineRequest.UnitCost))
            .When(x => x.UnitCost.HasValue);
        line.RuleFor(x => x.Direction).IsInEnum().When(x => x.Direction.HasValue);
    }
}
