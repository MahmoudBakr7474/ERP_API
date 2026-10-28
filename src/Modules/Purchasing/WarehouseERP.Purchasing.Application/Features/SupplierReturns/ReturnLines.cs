using FluentValidation;
using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns;

// A return's lines as the client sends them, shared by the create and update use cases.
internal static class ReturnLines
{
    public static void Rules(InlineValidator<ReturnLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.GoodsReceiptLineId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.LocationId).NotEqual(Guid.Empty).When(x => x.LocationId.HasValue);
    }

    public static IReadOnlyList<ReturnLineInput> Inputs(IReadOnlyList<ReturnLineRequest> lines) =>
        lines.Select(l => new ReturnLineInput(l.GoodsReceiptLineId, l.Quantity, l.LocationId)).ToList();
}
