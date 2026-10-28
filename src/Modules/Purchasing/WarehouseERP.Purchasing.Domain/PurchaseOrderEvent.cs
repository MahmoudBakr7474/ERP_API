using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>One step in a purchase order's history.</summary>
public sealed class PurchaseOrderEvent : DocumentEvent
{
    private PurchaseOrderEvent() { } // EF Core

    internal static PurchaseOrderEvent Create(PurchasingDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new PurchaseOrderEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
