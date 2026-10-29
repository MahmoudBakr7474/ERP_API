using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

public static class StockReservationDomainErrors
{
    public static Error Exceeded(int line, decimal remaining) => Error.BusinessRule(
        "Inventory.Reservation.Exceeded",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: the order line's reservation holds only {remaining} base units."));
}
