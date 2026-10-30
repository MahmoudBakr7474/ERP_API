using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings;

// Reads a document from its module (read-only contracts) and applies its posting rule (SourceEntries). A document its
// module no longer reports as posted fails visibly instead of being skipped.
public sealed class SourceEntryBuilder(
    IInventoryAccountingSource inventory, IPurchasingAccountingSource purchasing, ISalesAccountingSource sales, IPosAccountingSource pos,
    ICashAccountRepository cashAccounts)
{
    public async Task<Result<SourceEntry>> BuildAsync(SourcePosting posting, int decimals, CancellationToken ct)
    {
        var missing = SourcePostingErrors.DocumentNotFound(posting.SourceType, posting.SourceNumber);
        switch (posting.SourceType)
        {
            case SourceDocumentType.InventoryDocument:
                return await inventory.GetDocumentAsync(posting.SourceId, ct) is { } document
                    ? SourceEntries.InventoryDocument(document, decimals)
                    : missing;
            case SourceDocumentType.GoodsReceipt:
                return await purchasing.GetGoodsReceiptAsync(posting.SourceId, ct) is { } receipt
                    ? SourceEntries.GoodsReceipt(receipt, await StockValueAsync(receipt.StockDocumentId, ct))
                    : missing;
            case SourceDocumentType.SupplierInvoice:
                return await purchasing.GetSupplierInvoiceAsync(posting.SourceId, ct) is { } supplierInvoice
                    ? SourceEntries.SupplierInvoice(supplierInvoice)
                    : missing;
            case SourceDocumentType.SupplierReturn:
                return await purchasing.GetSupplierReturnAsync(posting.SourceId, ct) is { } supplierReturn
                    ? SourceEntries.SupplierReturn(supplierReturn, await StockValueAsync(supplierReturn.StockDocumentId, ct), decimals)
                    : missing;
            case SourceDocumentType.SalesDelivery:
                return await sales.GetDeliveryAsync(posting.SourceId, ct) is { } delivery
                    ? SourceEntries.SalesDelivery(delivery, await StockValueAsync(delivery.StockDocumentId, ct), decimals)
                    : missing;
            case SourceDocumentType.SalesInvoice:
                return await sales.GetInvoiceAsync(posting.SourceId, ct) is { } salesInvoice
                    ? SourceEntries.SalesInvoice(salesInvoice)
                    : missing;
            case SourceDocumentType.SalesReturn:
                return await sales.GetReturnAsync(posting.SourceId, ct) is { } salesReturn
                    ? SourceEntries.SalesReturn(salesReturn, await StockValueAsync(salesReturn.StockDocumentId, ct), decimals)
                    : missing;
            case SourceDocumentType.PosSale:
                return await pos.GetSaleAsync(posting.SourceId, ct) is { } sale
                    ? SourceEntries.PosSale(sale, sale.StockDocumentId is { } saleStock ? await StockValueAsync(saleStock, ct) : null, decimals)
                    : missing;
            case SourceDocumentType.PosReturn:
                return await pos.GetReturnAsync(posting.SourceId, ct) is { } posReturn
                    ? SourceEntries.PosReturn(posReturn, posReturn.StockDocumentId is { } returnStock ? await StockValueAsync(returnStock, ct) : null,
                        decimals)
                    : missing;
            case SourceDocumentType.PosShift:
                var shift = await pos.GetClosedShiftAsync(posting.SourceId, ct);
                if (shift is null)
                    return missing;
                var cashbox = await cashAccounts.FindPosCashboxAsync(shift.BranchId, ct);
                return cashbox is null ? SourcePostingErrors.PosCashboxMissing(shift.Number) : SourceEntries.PosShift(shift, cashbox.AccountId);
            default:
                throw new InvalidOperationException($"Source posting {posting.Id} has an unknown type {posting.SourceType}.");
        }
    }

    private async Task<decimal> StockValueAsync(Guid stockDocumentId, CancellationToken ct) =>
        (await inventory.GetDocumentValuesAsync([stockDocumentId], ct)).GetValueOrDefault(stockDocumentId);
}
