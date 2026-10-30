using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>One step in a journal's history: who did what and when, with a reason or a related journal's number. Never changes.</summary>
public sealed class JournalEvent
{
    private JournalEvent() { } // EF Core

    public long Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public JournalAction Action { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? Note { get; private set; }

    internal static JournalEvent Create(JournalAction action, Guid? userId, DateTime atUtc, string? note) =>
        new() { Action = action, UserId = userId, AtUtc = atUtc, Note = note };
}
