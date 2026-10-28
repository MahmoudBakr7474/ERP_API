using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

internal sealed class GoodsReceiptReadStore(DbContext db, ReferenceNames names) : IGoodsReceiptReadStore
{
    public async Task<GoodsReceiptDetailsResponse?> GetAsync(Guid receiptId, CancellationToken ct)
    {
        var receipt = await db.Set<GoodsReceipt>().AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == receiptId, ct);
        if (receipt is null)
            return null;

        var order = await db.Set<PurchaseOrder>().AsNoTracking().Where(o => o.Id == receipt.PurchaseOrderId)
            .Select(o => new { o.Number, o.CurrencyCode }).SingleAsync(ct);
        var supplier = await db.Set<Supplier>().AsNoTracking().Where(s => s.Id == receipt.SupplierId)
            .Select(s => new { s.Code, s.NameAr }).SingleAsync(ct);
        var returns = (await db.Set<SupplierReturn>().AsNoTracking().Where(r => r.GoodsReceiptId == receiptId)
                .Select(r => new { r.Id, r.Number, r.DraftNumber, r.Status, r.CreatedAtUtc, r.CreditClaimTotalAmount }).ToListAsync(ct))
            .Select(r => new LinkedDocumentResponse(r.Id, PurchasingDocumentType.SupplierReturn,
                r.Number ?? PurchasingNumbers.DraftReference(PurchasingDocumentType.SupplierReturn, r.DraftNumber), r.Status.ToString(),
                r.CreatedAtUtc, r.CreditClaimTotalAmount))
            .ToList();
        var lines = receipt.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId), warehouseIds: [receipt.WarehouseId],
            locationIds: lines.Select(l => l.LocationId), userIds: [receipt.ReceivedByUserId], ct: ct);

        return new GoodsReceiptDetailsResponse(
            receipt.Id, receipt.Number, receipt.PurchaseOrderId, order.Number ?? "", receipt.SupplierId, supplier.Code, supplier.NameAr,
            receipt.WarehouseId, lookup.WarehouseCode(receipt.WarehouseId), receipt.BranchId, order.CurrencyCode, receipt.SupplierDeliveryNote,
            receipt.Notes, receipt.StockDocumentId, receipt.StockDocumentNumber, receipt.ReceivedAtUtc, receipt.ReceivedByUserId,
            lookup.UserName(receipt.ReceivedByUserId),
            lines.Select(l => new GoodsReceiptLineResponse(
                l.Id, l.LineNumber, l.PurchaseOrderLineId, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId),
                l.ProductUnitId, lookup.UnitCode(l.ProductUnitId), l.Quantity, l.UnitCost, l.LocationId, lookup.LocationCode(l.LocationId),
                l.ReturnedQuantity, l.ReturnableQuantity)).ToList(),
            returns);
    }

    public async Task<PagedResult<GoodsReceiptSummaryResponse>> ListAsync(GoodsReceiptFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<GoodsReceipt>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.SupplierId is { } supplierId)
            query = query.Where(x => x.SupplierId == supplierId);
        if (filter.PurchaseOrderId is { } orderId)
            query = query.Where(x => x.PurchaseOrderId == orderId);
        if (filter.Search is { } search)
            query = query.Where(x => x.Number.Contains(search) || x.StockDocumentNumber.Contains(search)
                                     || (x.SupplierDeliveryNote != null && x.SupplierDeliveryNote.Contains(search)));

        var total = await query.CountAsync(ct);
        var rows = await query
            .Join(db.Set<PurchaseOrder>(), r => r.PurchaseOrderId, o => o.Id, (r, o) => new { Receipt = r, OrderNumber = o.Number })
            .Join(db.Set<Supplier>(), x => x.Receipt.SupplierId, s => s.Id, (x, s) => new
            {
                x.Receipt.Id, x.Receipt.Number, x.Receipt.PurchaseOrderId, x.OrderNumber, x.Receipt.SupplierId, SupplierName = s.NameAr,
                x.Receipt.WarehouseId, x.Receipt.StockDocumentNumber, x.Receipt.SupplierDeliveryNote, x.Receipt.ReceivedAtUtc,
                x.Receipt.ReceivedByUserId,
            })
            .OrderByDescending(x => x.ReceivedAtUtc).ThenByDescending(x => x.Number)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), userIds: rows.Select(r => r.ReceivedByUserId), ct: ct);
        return new PagedResult<GoodsReceiptSummaryResponse>(
            rows.Select(r => new GoodsReceiptSummaryResponse(
                r.Id, r.Number, r.PurchaseOrderId, r.OrderNumber ?? "", r.SupplierId, r.SupplierName, r.WarehouseId,
                lookup.WarehouseCode(r.WarehouseId), r.StockDocumentNumber, r.SupplierDeliveryNote, r.ReceivedAtUtc,
                lookup.UserName(r.ReceivedByUserId))).ToList(),
            page, pageSize, total);
    }

    public Task<GoodsReceiptPostedResponse?> FindPostedAsync(Guid receiptId, CancellationToken ct) =>
        db.Set<GoodsReceipt>().AsNoTracking()
            .Where(x => x.Id == receiptId)
            .Select(x => new GoodsReceiptPostedResponse(x.Id, x.Number, x.StockDocumentId, x.StockDocumentNumber, x.ReceivedAtUtc))
            .SingleOrDefaultAsync(ct);
}
