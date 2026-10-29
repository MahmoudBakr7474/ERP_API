namespace WarehouseERP.Inventory.Domain;

/// <summary>What a reservation entry did.</summary>
public enum ReservationEntryKind : byte
{
    /// <summary>The order was confirmed and its line reserved.</summary>
    Reserved = 1,

    /// <summary>A posted delivery took stock out of the reservation.</summary>
    Consumed = 2,

    /// <summary>A reduced, closed or cancelled order gave stock back.</summary>
    Released = 3,
}
