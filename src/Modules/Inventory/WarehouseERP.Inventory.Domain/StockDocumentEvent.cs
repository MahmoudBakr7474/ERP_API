using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>One step in a document's history: who did what and when, with the reason of a rejection. Never changes.</summary>
public sealed class StockDocumentEvent
{
    private StockDocumentEvent() { } // EF Core

    public long Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public StockDocumentAction Action { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? Note { get; private set; }

    internal static StockDocumentEvent Create(StockDocumentAction action, Guid? userId, DateTime atUtc, string? note) =>
        new() { Action = action, UserId = userId, AtUtc = atUtc, Note = note };
}
