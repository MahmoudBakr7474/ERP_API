using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

// Reserving stock for sales orders and giving it back; a reservation's own invariants are StockReservationDomainErrors.
public static class ReservationErrors
{
    public static readonly Error AlreadyReserved = Error.Conflict(
        "Inventory.Reservation.AlreadyReserved", "A line of this order already holds a reservation.");

    public static readonly Error WarehouseNotSelling = Error.BusinessRule(
        "Inventory.Reservation.WarehouseNotSelling", "Stock can be reserved only in a physical warehouse that allows selling.");

    public static Error NotFound(int line) => Error.BusinessRule(
        "Inventory.Reservation.NotFound", $"Line {line}: the order line holds no reservation of this product in this warehouse.");
}
