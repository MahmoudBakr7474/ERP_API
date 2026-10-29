using FluentValidation;
using WarehouseERP.Sales.Application.Contracts.Returns;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns;

// A return's lines as the client sends them: their shape (shared by the create and update validators), the delivery line's
// product unit with Catalog's fraction rules, and the restocking location, an active location of the delivery's warehouse
// (DEFAULT when none is named). Conditions other than fit for resale are refused by the domain.
internal static class ReturnLines
{
    public static void Rules(InlineValidator<SalesReturnLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.DeliveryLineId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.Condition).IsInEnum();
        line.RuleFor(x => x.LocationId).NotEqual(Guid.Empty).When(x => x.LocationId.HasValue);
    }

    public static async Task<Result<IReadOnlyList<ReturnLineInput>>> ResolveAsync(
        SalesReferences references, SalesDelivery delivery, IReadOnlyList<SalesReturnLineRequest> lines, CancellationToken ct)
    {
        var deliveryLines = new List<SalesDeliveryLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            if (delivery.FindLine(lines[i].DeliveryLineId) is not { } deliveryLine)
                return SalesDeliveryDomainErrors.LineNotFound(i + 1);
            deliveryLines.Add(deliveryLine);
        }
        var locations = await references.ResolveLocationsAsync(delivery.WarehouseId, lines.Select(l => l.LocationId).ToList(), ct);
        if (locations.IsFailure)
            return locations.Error;
        var units = await references.UnitFactsAsync(deliveryLines.Select(l => l.ProductUnitId).ToList(), ct);
        return lines.Select((l, i) => new ReturnLineInput(
                l.DeliveryLineId, units[deliveryLines[i].ProductUnitId], l.Quantity, l.Condition, locations.Value[i]))
            .ToList();
    }
}
