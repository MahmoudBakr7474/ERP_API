using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>One step in a sales order's history.</summary>
public sealed class SalesOrderEvent : DocumentEvent
{
    private SalesOrderEvent() { } // EF Core

    internal static SalesOrderEvent Create(SalesDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new SalesOrderEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
