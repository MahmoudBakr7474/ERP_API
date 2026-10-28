namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <param name="MayOverReserve">
/// True for authorized decreases (adjustments, reversals): they may take sellable stock below the reserved quantity
/// (recorded as an over-reservation), never below zero. Ordinary issues may only take unreserved stock.
/// </param>
public sealed record StockPosting(Guid DocumentId, DateTime PostedAtUtc, bool MayOverReserve, IReadOnlyList<StockChange> Changes);
