using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>One step in a purchase request's history.</summary>
public sealed class PurchaseRequestEvent : DocumentEvent
{
    private PurchaseRequestEvent() { } // EF Core

    internal static PurchaseRequestEvent Create(PurchasingDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new PurchaseRequestEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
