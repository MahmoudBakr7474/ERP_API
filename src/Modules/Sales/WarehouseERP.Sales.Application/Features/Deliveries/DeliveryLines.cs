using FluentValidation;
using WarehouseERP.Sales.Application.Contracts.Deliveries;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Deliveries;

// A delivery's picks as the client sends them: their shape (shared by the create and update validators), the order line's
// product unit with Catalog's fraction rules (active or not: goods already sold are still delivered), and the locations,
// each an active location of the order's warehouse (DEFAULT when none is named).
internal static class DeliveryLines
{
    public static void Rules(InlineValidator<DeliveryLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.OrderLineId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.LocationId).NotEqual(Guid.Empty).When(x => x.LocationId.HasValue);
    }

    public static async Task<Result<IReadOnlyList<DeliveryLineInput>>> ResolveAsync(
        SalesReferences references, SalesOrder order, IReadOnlyList<DeliveryLineRequest> lines, CancellationToken ct)
    {
        var orderLines = new List<SalesOrderLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            if (order.FindLine(lines[i].OrderLineId) is not { } orderLine)
                return SalesOrderDomainErrors.LineNotFound(i + 1);
            orderLines.Add(orderLine);
        }
        var locations = await references.ResolveLocationsAsync(order.WarehouseId, lines.Select(l => l.LocationId).ToList(), ct);
        if (locations.IsFailure)
            return locations.Error;
        var units = await references.UnitFactsAsync(orderLines.Select(l => l.ProductUnitId).ToList(), ct);
        return lines.Select((l, i) => new DeliveryLineInput(l.OrderLineId, units[orderLines[i].ProductUnitId], l.Quantity, locations.Value[i]))
            .ToList();
    }
}
