using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders;

// What an order takes from approved purchase requests. Each order line may name one request line of the same warehouse,
// product and unit; the request line records the quantity on order and never goes above what was approved. Saving a
// draft order moves the difference between its old and new lines; cancelling it gives everything back. The requests are
// locked in id order inside the caller's transaction, so two orders drawing on the same line can't both take its rest.
internal static class RequestDraws
{
    /// <summary>What the lines take, by request line id.</summary>
    public static Dictionary<Guid, RequestDraw> Of(IEnumerable<PurchaseOrderLine> lines) =>
        lines.Where(l => l.PurchaseRequestLineId.HasValue)
            .ToDictionary(l => l.PurchaseRequestLineId!.Value, l => new RequestDraw(l.Quantity, l.LineNumber, l.ProductUnitId));

    public static async Task<Result> MoveAsync(
        IPurchaseRequestRepository requests, Guid warehouseId, IReadOnlyDictionary<Guid, RequestDraw> before,
        IReadOnlyDictionary<Guid, RequestDraw> after, CancellationToken ct)
    {
        var lineIds = before.Keys.Union(after.Keys).ToList();
        if (lineIds.Count == 0)
            return Result.Success();

        var locked = await requests.GetByLinesForUpdateAsync(lineIds, ct);
        foreach (var (lineId, draw) in after)
        {
            var request = locked.SingleOrDefault(r => r.FindLine(lineId) is not null);
            if (request is null)
                return PurchaseRequestErrors.LineNotFound(draw.OrderLineNumber);
            if (request.WarehouseId != warehouseId)
                return PurchaseRequestErrors.WarehouseMismatch(draw.OrderLineNumber);
            if (request.FindLine(lineId)!.ProductUnitId != draw.ProductUnitId)
                return PurchaseRequestErrors.ProductMismatch(draw.OrderLineNumber);
        }

        foreach (var request in locked)
        {
            var changes = lineIds
                .Where(id => request.FindLine(id) is not null)
                .Select(id => new RequestAllocation(
                    id, Quantity(after, id) - Quantity(before, id),
                    after.TryGetValue(id, out var now) ? now.OrderLineNumber : before[id].OrderLineNumber))
                .Where(c => c.Quantity != 0)
                .ToList();
            if (changes.Count == 0)
                continue;
            var changed = request.ChangeOrdered(changes);
            if (changed.IsFailure)
                return changed;
        }
        return Result.Success();
    }

    private static decimal Quantity(IReadOnlyDictionary<Guid, RequestDraw> draws, Guid lineId) =>
        draws.TryGetValue(lineId, out var draw) ? draw.Quantity : 0m;
}
