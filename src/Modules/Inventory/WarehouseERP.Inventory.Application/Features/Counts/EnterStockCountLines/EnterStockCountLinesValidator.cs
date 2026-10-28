using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.Counts.EnterStockCountLines;

public sealed class EnterStockCountLinesValidator : AbstractValidator<EnterStockCountLinesCommand>
{
    private const int MaxLines = 5000;

    public EnterStockCountLinesValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.CountId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= MaxLines).WithMessage("Enter at most 5000 lines at once.")
            .Must(l => l.Select(x => (x.ProductId, x.LocationId)).Distinct().Count() == l.Count)
            .WithMessage("Enter each product and location once.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(line =>
        {
            line.RuleLevelCascadeMode = CascadeMode.Stop;
            line.RuleFor(x => x.ProductId).NotEmpty();
            line.RuleFor(x => x.LocationId).NotEmpty();
            line.RuleFor(x => x.CountedQuantity)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(Quantities.MaxQuantity)
                .Must(q => Quantities.FitsScale(q, Quantities.QuantityScale))
                .WithMessage("'Counted Quantity' may have at most 4 decimals.");
            line.RuleFor(x => x.UnitCost!.Value)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(Quantities.MaxUnitCost)
                .Must(c => Quantities.FitsScale(c, Quantities.CostScale))
                .WithMessage("'Unit Cost' may have at most 8 decimals.")
                .OverridePropertyName(nameof(CountedLineRequest.UnitCost))
                .When(x => x.UnitCost.HasValue);
        });
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
