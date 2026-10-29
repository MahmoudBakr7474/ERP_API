using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

// A quotation line as the API shows it.
internal static class PricedLines
{
    public static PricedLineResponse Response(PricedLine l, NameLookup lookup) => new(
        l.Id, l.LineNumber, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), l.ProductUnitId,
        lookup.UnitCode(l.ProductUnitId), l.ConversionFactor, l.Quantity, l.UnitPrice, l.PriceSource, l.ListUnitPrice, l.DiscountPercent,
        l.TaxTreatment, l.TaxRate, l.GrossAmount, l.DiscountAmount, l.NetAmount, l.TaxAmount, l.TotalAmount, l.NetUnitPrice);

    public static SalesDocumentEventResponse Event(DocumentEvent e, NameLookup lookup) =>
        new(e.Action, e.UserId, lookup.UserName(e.UserId), e.AtUtc, e.Note);
}
