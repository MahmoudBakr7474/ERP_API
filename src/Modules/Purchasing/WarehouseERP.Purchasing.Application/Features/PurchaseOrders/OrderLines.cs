using FluentValidation;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders;

// An order's lines as the client sends them: their shape (shared by the create and update validators) and their product
// units, checked in Catalog. Amounts and quantity rules are the domain's.
internal static class OrderLines
{
    public static void Rules(InlineValidator<OrderLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.ProductId).NotEmpty();
        line.RuleFor(x => x.ProductUnitId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).LessThanOrEqualTo(Amounts.MaxUnitPrice)
            .Must(p => Amounts.FitsScale(p, Amounts.PriceScale)).WithMessage("'Unit Price' may have at most 8 decimals.");
        line.RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100)
            .Must(p => Amounts.FitsScale(p, Amounts.PercentScale)).WithMessage("'Discount Percent' may have at most 4 decimals.");
        line.RuleFor(x => x.TaxRate).InclusiveBetween(0, 100)
            .Must(p => Amounts.FitsScale(p, Amounts.PercentScale)).WithMessage("'Tax Rate' may have at most 4 decimals.");
        line.RuleFor(x => x.TaxTreatment).IsInEnum();
        line.RuleFor(x => x.PurchaseRequestLineId).NotEqual(Guid.Empty).When(x => x.PurchaseRequestLineId.HasValue);
    }

    public static async Task<Result<IReadOnlyList<OrderLineInput>>> ResolveAsync(
        PurchasingReferences references, IReadOnlyList<OrderLineRequest> lines, CancellationToken ct)
    {
        var units = await references.ResolveUnitsAsync(lines.Select(l => new ProductUnitRef(l.ProductId, l.ProductUnitId)).ToList(), ct);
        if (units.IsFailure)
            return units.Error;
        return lines.Select((l, i) => new OrderLineInput(
                units.Value[i], l.Quantity, new LinePrice(l.UnitPrice, l.DiscountPercent, l.TaxRate, l.TaxTreatment), l.PurchaseRequestLineId))
            .ToList();
    }
}
