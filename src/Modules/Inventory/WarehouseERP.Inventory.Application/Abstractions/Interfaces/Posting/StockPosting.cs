namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <param name="MayOverReserve">
/// True for authorized decreases (adjustments, reversals): they may take sellable stock below the reserved quantity
/// (recorded as an over-reservation), never below zero. Ordinary issues may only take unreserved stock.
/// </param>
/// <param name="ConsumesReservations">
/// True for a sales delivery: its decreases take reserved stock, lowering on hand and reserved together, and still need
/// the stock to be physically there.
/// </param>
public sealed record StockPosting(
    Guid DocumentId, DateTime PostedAtUtc, bool MayOverReserve, bool ConsumesReservations, IReadOnlyList<StockChange> Changes);
