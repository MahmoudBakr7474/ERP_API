namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// How Sales holds sellable stock for a confirmed order and gives it back. Inventory owns the reservation: each source
/// line (a sales order line) has one reservation row, and a warehouse's ReservedQuantity always equals what its
/// reservations still hold. Call it inside the caller's database transaction; a refusal leaves the caller to roll back,
/// so nothing is ever half reserved. Deliveries consume reservations through <see cref="ISalesStockPosting"/>.
/// </summary>
public interface IStockReservations
{
    /// <summary>
    /// Reserves every line in base units, or refuses: in an active physical warehouse that allows selling, taking only
    /// stock that is on hand and not reserved by anyone else, under the warehouse's inventory lock.
    /// </summary>
    Task<StockReservationOutcome> ReserveAsync(StockReservationRequest request, CancellationToken ct);

    /// <summary>Gives back part or all of what the lines' reservations still hold (a reduced, closed or cancelled order).</summary>
    Task<StockReservationOutcome> ReleaseAsync(StockReleaseRequest request, CancellationToken ct);
}
