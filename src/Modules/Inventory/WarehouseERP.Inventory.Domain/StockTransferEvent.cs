using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>One step in a transfer's history: who did what and when, with a reason or a document number. Never changes.</summary>
public sealed class StockTransferEvent
{
    private StockTransferEvent() { } // EF Core

    public long Id { get; private set; }
    public Guid TransferId { get; private set; }
    public StockTransferAction Action { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? Note { get; private set; }

    internal static StockTransferEvent Create(StockTransferAction action, Guid? userId, DateTime atUtc, string? note) =>
        new() { Action = action, UserId = userId, AtUtc = atUtc, Note = note };
}
