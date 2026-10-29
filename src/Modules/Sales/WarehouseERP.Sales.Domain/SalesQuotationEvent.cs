using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>One step in a quotation's history.</summary>
public sealed class SalesQuotationEvent : DocumentEvent
{
    private SalesQuotationEvent() { } // EF Core

    internal static SalesQuotationEvent Create(SalesDocumentAction action, Guid? userId, DateTime atUtc, string? note)
    {
        var created = new SalesQuotationEvent();
        created.Fill(action, userId, atUtc, note);
        return created;
    }
}
