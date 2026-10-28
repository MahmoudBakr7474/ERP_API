using FluentValidation;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests;

// A request's lines as the client sends them, shared by the create and update use cases.
internal static class RequestLines
{
    public static void Rules(InlineValidator<RequestLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.ProductId).NotEmpty();
        line.RuleFor(x => x.ProductUnitId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.Notes).MaximumLength(200);
    }

    public static async Task<Result<IReadOnlyList<RequestLineInput>>> ResolveAsync(
        PurchasingReferences references, IReadOnlyList<RequestLineRequest> lines, CancellationToken ct)
    {
        var units = await references.ResolveUnitsAsync(lines.Select(l => new ProductUnitRef(l.ProductId, l.ProductUnitId)).ToList(), ct);
        if (units.IsFailure)
            return units.Error;
        return lines.Select((l, i) => new RequestLineInput(units.Value[i], l.Quantity, l.Notes)).ToList();
    }
}
