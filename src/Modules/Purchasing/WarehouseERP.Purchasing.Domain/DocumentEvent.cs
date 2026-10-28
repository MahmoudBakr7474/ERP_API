using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// One step in a purchasing document's history: who did what and when, with a reason or the number of a related
/// document. Never changes. Each document type keeps its own events table.
/// </summary>
public abstract class DocumentEvent
{
    public long Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public PurchasingDocumentAction Action { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? Note { get; private set; }

    protected void Fill(PurchasingDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        Action = action;
        UserId = userId;
        AtUtc = atUtc;
        Note = note;
    }
}
