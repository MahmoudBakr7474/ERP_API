using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Reservations;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Reservations;

// Sales holds stock for a confirmed order and gives it back through IStockReservations, inside its own transaction.
// Reserving takes the warehouse's inventory lock (shared, like a posting, so a deactivation or a count start can't slip
// in), checks the warehouse may sell, then raises ReservedQuantity product by product in a fixed order with conditional
// updates that take only stock on hand and not reserved yet: two orders racing for the last unit can't both win, nor can
// an order and an ordinary issue. Releasing locks the reservation rows first, then the warehouse, then gives its total
// back. Nothing is saved here: the caller's save point writes the reservations with its own document, and a refusal
// leaves the caller to roll back what was already reserved.
public sealed partial class StockReservations(
    IStockReservationRepository reservations, StockReferences references, IInventoryLocks locks, IStockLedger ledger,
    IProductCatalog products, ICurrentUser currentUser, TimeProvider clock, ILogger<StockReservations> logger)
    : IStockReservations
{
    public async Task<StockReservationOutcome> ReserveAsync(StockReservationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines.Count == 0)
            throw new ArgumentException("A reservation holds at least one line.", nameof(request));
        if (request.Lines.Select(l => l.SourceLineId).Distinct().Count() != request.Lines.Count)
            throw new ArgumentException("Each order line is reserved once.", nameof(request));

        var reserved = await ReserveOrRefuseAsync(request, ct);
        if (reserved.IsFailure)
            return Refused(reserved.Error);
        StockReserved(logger, request.SourceDocumentId, request.Lines.Count, request.WarehouseId);
        return StockReservationOutcome.Done(reserved.Value);
    }

    public async Task<StockReservationOutcome> ReleaseAsync(StockReleaseRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Reason);
        if (request.Lines.Count == 0)
            throw new ArgumentException("A release gives back at least one line.", nameof(request));
        if (request.Lines.Select(l => l.SourceLineId).Distinct().Count() != request.Lines.Count)
            throw new ArgumentException("Each order line is released once per request.", nameof(request));

        var released = await ReleaseOrRefuseAsync(request, ct);
        if (released.IsFailure)
            return Refused(released.Error);
        StockReleased(logger, request.SourceDocumentId, request.Lines.Count);
        return StockReservationOutcome.Done(released.Value);
    }

    private async Task<Result<IReadOnlyList<ReservedQuantity>>> ReserveOrRefuseAsync(StockReservationRequest request, CancellationToken ct)
    {
        if (await reservations.AnyForLinesAsync(request.Lines.Select(l => l.SourceLineId).ToList(), ct))
            return ReservationErrors.AlreadyReserved;
        var lines = await references.ResolveLinesAsync(StockDocumentType.SalesDelivery, request.WarehouseId,
            request.Lines.Select(l => new StockLineRequest(l.ProductId, l.ProductUnitId, l.Quantity)).ToList(), ct);
        if (lines.IsFailure)
            return lines.Error;
        var baseQuantities = new decimal[lines.Value.Count];
        for (var i = 0; i < lines.Value.Count; i++)
        {
            var line = lines.Value[i];
            var baseQuantity = Quantities.ToBase(
                line.Quantity, line.ConversionFactor, line.UnitAllowsFraction, line.BaseUnitAllowsFraction, i + 1);
            if (baseQuantity.IsFailure)
                return baseQuantity.Error;
            baseQuantities[i] = baseQuantity.Value;
        }

        if (!await locks.TryLockWarehousesAsync([request.WarehouseId], exclusive: false, ct))
            return StockErrors.Busy;
        var warehouse = await references.UsableWarehouseAsync(request.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        if (!warehouse.Value.AllowsSelling)
            return ReservationErrors.WarehouseNotSelling;

        var byProduct = request.Lines.Select((l, i) => (l.ProductId, Quantity: baseQuantities[i]))
            .GroupBy(x => x.ProductId)
            .OrderBy(g => g.Key);
        foreach (var product in byProduct)
            if (!await ledger.TryReserveAsync(product.Key, request.WarehouseId, product.Sum(x => x.Quantity), ct))
                return StockErrors.Insufficient((await products.FindAsync(product.Key, ct))!.Sku);

        var userId = currentUser.UserId;
        var now = clock.GetUtcNow().UtcDateTime;
        var result = new List<ReservedQuantity>(request.Lines.Count);
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            reservations.Add(StockReservation.Reserve(
                request.SourceDocumentId, line.SourceLineId, request.SourceNumber, line.ProductId, request.WarehouseId,
                baseQuantities[i], userId, now));
            result.Add(new ReservedQuantity(line.SourceLineId, baseQuantities[i]));
        }
        return result;
    }

    private async Task<Result<IReadOnlyList<ReservedQuantity>>> ReleaseOrRefuseAsync(StockReleaseRequest request, CancellationToken ct)
    {
        var held = (await reservations.GetForUpdateAsync(request.Lines.Select(l => l.SourceLineId).ToList(), ct))
            .ToDictionary(r => r.SourceLineId);
        for (var i = 0; i < request.Lines.Count; i++)
            if (!held.TryGetValue(request.Lines[i].SourceLineId, out var reservation)
                || reservation.SourceDocumentId != request.SourceDocumentId)
                return ReservationErrors.NotFound(i + 1);
        if (!await locks.TryLockWarehousesAsync(held.Values.Select(r => r.WarehouseId).Distinct().ToList(), exclusive: false, ct))
            return StockErrors.Busy;

        var userId = currentUser.UserId;
        var now = clock.GetUtcNow().UtcDateTime;
        var result = new List<ReservedQuantity>(request.Lines.Count);
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            var released = held[line.SourceLineId].Release(line.BaseQuantity, request.Reason, userId, now, i + 1);
            if (released.IsFailure)
                return released.Error;
            result.Add(new ReservedQuantity(line.SourceLineId, released.Value));
        }

        var totals = request.Lines.Select((l, i) => (Reservation: held[l.SourceLineId], result[i].BaseQuantity))
            .GroupBy(x => (x.Reservation.WarehouseId, x.Reservation.ProductId))
            .Select(g => (g.Key.WarehouseId, g.Key.ProductId, Quantity: g.Sum(x => x.BaseQuantity)))
            .Where(x => x.Quantity > 0)
            .OrderBy(x => x.WarehouseId).ThenBy(x => x.ProductId);
        foreach (var (warehouseId, productId, quantity) in totals)
            await ledger.ReleaseReservedAsync(productId, warehouseId, quantity, ct);
        return result;
    }

    private static StockReservationOutcome Refused(Error error) =>
        StockReservationOutcome.Refused(error.Code, error.Message, error.Type == ErrorType.Conflict);

    [LoggerMessage(EventId = 5050, Level = LogLevel.Information,
        Message = "Sales order {SourceDocumentId} reserved {LineCount} lines in warehouse {WarehouseId}")]
    private static partial void StockReserved(ILogger logger, Guid sourceDocumentId, int lineCount, Guid warehouseId);

    [LoggerMessage(EventId = 5051, Level = LogLevel.Information,
        Message = "Sales order {SourceDocumentId} released the reservations of {LineCount} lines")]
    private static partial void StockReleased(ILogger logger, Guid sourceDocumentId, int lineCount);
}
