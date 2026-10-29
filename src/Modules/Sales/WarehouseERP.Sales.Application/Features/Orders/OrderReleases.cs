using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders;

// Gives Inventory back the reservation of what an order will never deliver (cancelled, closed or reduced), inside the
// caller's transaction, in base units: the line's quantity times the conversion factor it was reserved with.
internal static class OrderReleases
{
    public static async Task<Result> ReleaseAsync(
        IStockReservations reservations, SalesOrder order, IReadOnlyList<OrderLineQuantity> lines, string reason, CancellationToken ct)
    {
        var released = lines.Where(l => l.Quantity > 0)
            .Select(l => new StockReleaseLine(l.OrderLineId, l.Quantity * order.FindLine(l.OrderLineId)!.ConversionFactor))
            .ToList();
        if (released.Count == 0)
            return Result.Success();
        var outcome = await reservations.ReleaseAsync(new StockReleaseRequest(order.Id, reason, released), ct);
        return outcome.Succeeded ? Result.Success() : StockErrors.Refused(outcome);
    }
}
