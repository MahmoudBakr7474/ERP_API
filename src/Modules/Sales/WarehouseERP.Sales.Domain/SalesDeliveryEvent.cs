using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>One step in a delivery note's history.</summary>
public sealed class SalesDeliveryEvent : DocumentEvent
{
    private SalesDeliveryEvent() { } // EF Core

    internal static SalesDeliveryEvent Create(SalesDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new SalesDeliveryEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
