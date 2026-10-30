using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings;

// What each module reports as posted in a period, through its read-only contract, named as accounting sources.
public sealed class SourceDocuments(
    IInventoryAccountingSource inventory, IPurchasingAccountingSource purchasing, ISalesAccountingSource sales, IPosAccountingSource pos)
{
    public static IReadOnlyList<string> Modules { get; } = ["Inventory", "Purchasing", "Sales", "Pos"];

    public async Task<IReadOnlyList<SourceDocument>> ListAsync(string module, DateTime fromUtc, DateTime toUtc, CancellationToken ct) =>
        module switch
        {
            "Inventory" => (await inventory.ListPostedAsync(fromUtc, toUtc, ct))
                .Select(d => new SourceDocument(SourceDocumentType.InventoryDocument, d.DocumentId, d.Number, d.PostedAtUtc)).ToList(),
            "Purchasing" => (await purchasing.ListPostedAsync(fromUtc, toUtc, ct))
                .Select(d => new SourceDocument(d.DocumentType switch
                {
                    PurchasingDocumentType.GoodsReceipt => SourceDocumentType.GoodsReceipt,
                    PurchasingDocumentType.SupplierInvoice => SourceDocumentType.SupplierInvoice,
                    _ => SourceDocumentType.SupplierReturn,
                }, d.Id, d.Number, d.PostedAtUtc)).ToList(),
            "Sales" => (await sales.ListPostedAsync(fromUtc, toUtc, ct))
                .Select(d => new SourceDocument(d.DocumentType switch
                {
                    SalesDocumentType.Delivery => SourceDocumentType.SalesDelivery,
                    SalesDocumentType.Invoice => SourceDocumentType.SalesInvoice,
                    _ => SourceDocumentType.SalesReturn,
                }, d.Id, d.Number, d.PostedAtUtc)).ToList(),
            "Pos" => (await pos.ListPostedAsync(fromUtc, toUtc, ct))
                .Select(d => new SourceDocument(d.DocumentType switch
                {
                    PosDocumentType.Sale => SourceDocumentType.PosSale,
                    PosDocumentType.Return => SourceDocumentType.PosReturn,
                    _ => SourceDocumentType.PosShift,
                }, d.Id, d.Number, d.OccurredAtUtc)).ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, "Unknown module."),
        };
}
