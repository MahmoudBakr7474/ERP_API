using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Reservations;

/// <summary>Write side of reservations; saved by the caller's save point together with its own document.</summary>
public interface IStockReservationRepository
{
    /// <summary>True when any of these sales order lines already has a reservation.</summary>
    Task<bool> AnyForLinesAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken ct);

    /// <summary>
    /// The reservations of these sales order lines, tracked and locked against other writers until the transaction ends,
    /// taken in one fixed order; lines without one are left out.
    /// </summary>
    Task<IReadOnlyList<StockReservation>> GetForUpdateAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken ct);

    void Add(StockReservation reservation);
}
