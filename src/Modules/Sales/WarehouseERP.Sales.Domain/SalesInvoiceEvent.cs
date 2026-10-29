using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>One step in a sales invoice's history.</summary>
public sealed class SalesInvoiceEvent : DocumentEvent
{
    private SalesInvoiceEvent() { } // EF Core

    internal static SalesInvoiceEvent Create(SalesDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new SalesInvoiceEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
