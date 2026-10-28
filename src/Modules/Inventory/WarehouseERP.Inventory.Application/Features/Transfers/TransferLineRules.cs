using FluentValidation;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.Transfers;

// The shape of one transfer line, shared by the create and update validators.
internal static class TransferLineRules
{
    public static void Apply(InlineValidator<TransferLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.ProductId).NotEmpty();
        line.RuleFor(x => x.ProductUnitId).NotEmpty();
        line.RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(Quantities.MaxQuantity)
            .Must(q => Quantities.FitsScale(q, Quantities.QuantityScale))
            .WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.SourceLocationId).NotEqual(Guid.Empty).When(x => x.SourceLocationId.HasValue);
    }
}
