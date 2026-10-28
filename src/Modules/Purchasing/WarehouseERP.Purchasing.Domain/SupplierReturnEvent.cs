using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>One step in a supplier return's history.</summary>
public sealed class SupplierReturnEvent : DocumentEvent
{
    private SupplierReturnEvent() { } // EF Core

    internal static SupplierReturnEvent Create(PurchasingDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new SupplierReturnEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
