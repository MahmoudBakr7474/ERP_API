using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>One step in a customer return's history.</summary>
public sealed class SalesReturnEvent : DocumentEvent
{
    private SalesReturnEvent() { } // EF Core

    internal static SalesReturnEvent Create(SalesDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new SalesReturnEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
