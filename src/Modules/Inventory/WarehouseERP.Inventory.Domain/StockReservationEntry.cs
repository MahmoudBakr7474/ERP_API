namespace WarehouseERP.Inventory.Domain;

/// <summary>One change to a reservation: reserved, consumed by a delivery's stock document, or released with a reason. Never changes.</summary>
public sealed class StockReservationEntry
{
    private StockReservationEntry() { } // EF Core

    public long Id { get; private set; }
    public Guid ReservationId { get; private set; }
    public ReservationEntryKind Kind { get; private set; }

    /// <summary>Base units, always positive.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>The delivery's stock document, for a consumption.</summary>
    public Guid? StockDocumentId { get; private set; }

    public string? Note { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }

    internal static StockReservationEntry Create(
        ReservationEntryKind kind, decimal quantity, Guid? stockDocumentId, string? note, Guid? userId, DateTime atUtc) =>
        new() { Kind = kind, Quantity = quantity, StockDocumentId = stockDocumentId, Note = note, UserId = userId, AtUtc = atUtc };
}
