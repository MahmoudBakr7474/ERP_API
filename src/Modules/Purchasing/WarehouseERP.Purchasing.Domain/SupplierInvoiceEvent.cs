using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>One step in a supplier invoice's history.</summary>
public sealed class SupplierInvoiceEvent : DocumentEvent
{
    private SupplierInvoiceEvent() { } // EF Core

    internal static SupplierInvoiceEvent Create(PurchasingDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new SupplierInvoiceEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
