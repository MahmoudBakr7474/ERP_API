using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>One step in a count's history: who did what and when, with a reason or a document number. Never changes.</summary>
public sealed class StockCountEvent
{
    private StockCountEvent() { } // EF Core

    public long Id { get; private set; }
    public Guid CountId { get; private set; }
    public StockCountAction Action { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? Note { get; private set; }

    internal static StockCountEvent Create(StockCountAction action, Guid? userId, DateTime atUtc, string? note) =>
        new() { Action = action, UserId = userId, AtUtc = atUtc, Note = note };
}
