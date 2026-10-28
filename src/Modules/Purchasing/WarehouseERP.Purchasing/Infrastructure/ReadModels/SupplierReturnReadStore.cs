using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

internal sealed class SupplierReturnReadStore(DbContext db, ReferenceNames names) : ISupplierReturnReadStore
{
    public async Task<SupplierReturnDetailsResponse?> GetAsync(Guid returnId, CancellationToken ct)
    {
        var supplierReturn = await db.Set<SupplierReturn>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == returnId, ct);
        if (supplierReturn is null)
            return null;

        var receiptNumber = await db.Set<GoodsReceipt>().AsNoTracking().Where(r => r.Id == supplierReturn.GoodsReceiptId).Select(r => r.Number).SingleAsync(ct);
        var orderNumber = await db.Set<PurchaseOrder>().AsNoTracking().Where(o => o.Id == supplierReturn.PurchaseOrderId).Select(o => o.Number).SingleAsync(ct);
        var supplier = await db.Set<Supplier>().AsNoTracking().Where(s => s.Id == supplierReturn.SupplierId)
            .Select(s => new { s.Code, s.NameAr }).SingleAsync(ct);
        var lines = supplierReturn.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId),
            warehouseIds: [supplierReturn.WarehouseId], locationIds: lines.Select(l => l.LocationId),
            userIds: supplierReturn.Events.Select(e => e.UserId).Append(supplierReturn.CreatedByUserId)
                .Append(supplierReturn.SubmittedByUserId).Append(supplierReturn.ApprovedByUserId),
            ct: ct);

        return new SupplierReturnDetailsResponse(
            supplierReturn.Id, PurchasingNumbers.DraftReference(PurchasingDocumentType.SupplierReturn, supplierReturn.DraftNumber),
            supplierReturn.Number, supplierReturn.Status, supplierReturn.GoodsReceiptId, receiptNumber, supplierReturn.PurchaseOrderId,
            orderNumber ?? "", supplierReturn.SupplierId, supplier.Code, supplier.NameAr, supplierReturn.WarehouseId,
            lookup.WarehouseCode(supplierReturn.WarehouseId), supplierReturn.BranchId, supplierReturn.CurrencyCode, supplierReturn.Reason,
            supplierReturn.Notes, supplierReturn.StockDocumentId, supplierReturn.StockDocumentNumber, supplierReturn.CreditClaimNetAmount,
            supplierReturn.CreditClaimTaxAmount, supplierReturn.CreditClaimTotalAmount, supplierReturn.CreatedAtUtc,
            supplierReturn.CreatedByUserId, lookup.UserName(supplierReturn.CreatedByUserId), lookup.UserName(supplierReturn.SubmittedByUserId),
            lookup.UserName(supplierReturn.ApprovedByUserId), supplierReturn.PostedAtUtc, Convert.ToBase64String(supplierReturn.RowVersion),
            lines.Select(l => new SupplierReturnLineResponse(
                l.Id, l.LineNumber, l.GoodsReceiptLineId, l.PurchaseOrderLineId, l.ProductId, lookup.Sku(l.ProductId),
                lookup.ProductName(l.ProductId), l.ProductUnitId, lookup.UnitCode(l.ProductUnitId), l.Quantity, l.LocationId,
                lookup.LocationCode(l.LocationId), l.ReceiptUnitCost, l.UninvoicedQuantity, l.InvoicedQuantity, l.CreditClaimNetAmount,
                l.CreditClaimTaxAmount)).ToList(),
            supplierReturn.Events.OrderBy(e => e.Id)
                .Select(e => new DocumentEventResponse(e.Action, e.UserId, lookup.UserName(e.UserId), e.AtUtc, e.Note)).ToList());
    }

    public async Task<PagedResult<SupplierReturnSummaryResponse>> ListAsync(SupplierReturnFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SupplierReturn>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.SupplierId is { } supplierId)
            query = query.Where(x => x.SupplierId == supplierId);
        if (filter.PurchaseOrderId is { } orderId)
            query = query.Where(x => x.PurchaseOrderId == orderId);
        if (filter.GoodsReceiptId is { } receiptId)
            query = query.Where(x => x.GoodsReceiptId == receiptId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.WithCreditClaim is { } withClaim)
            query = query.Where(x => x.Status == SupplierReturnStatus.Posted && (x.CreditClaimTotalAmount > 0) == withClaim);
        if (filter.Search is { } search)
        {
            var draftNumber = ReferenceNames.DraftNumberOf(search);
            query = query.Where(x => (x.Number != null && x.Number.Contains(search)) || x.DraftNumber == draftNumber);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .Join(db.Set<GoodsReceipt>(), r => r.GoodsReceiptId, g => g.Id, (r, g) => new { Return = r, ReceiptNumber = g.Number })
            .Join(db.Set<PurchaseOrder>(), x => x.Return.PurchaseOrderId, o => o.Id, (x, o) => new { x.Return, x.ReceiptNumber, OrderNumber = o.Number })
            .Join(db.Set<Supplier>(), x => x.Return.SupplierId, s => s.Id, (x, s) => new { x.Return, x.ReceiptNumber, x.OrderNumber, SupplierName = s.NameAr })
            .OrderByDescending(x => x.Return.CreatedAtUtc).ThenByDescending(x => x.Return.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.Return.WarehouseId), ct: ct);
        return new PagedResult<SupplierReturnSummaryResponse>(
            rows.Select(r => new SupplierReturnSummaryResponse(
                r.Return.Id, PurchasingNumbers.DraftReference(PurchasingDocumentType.SupplierReturn, r.Return.DraftNumber), r.Return.Number,
                r.Return.Status, r.Return.GoodsReceiptId, r.ReceiptNumber, r.Return.PurchaseOrderId, r.OrderNumber ?? "", r.Return.SupplierId,
                r.SupplierName, r.Return.WarehouseId, lookup.WarehouseCode(r.Return.WarehouseId), r.Return.Reason,
                r.Return.CreditClaimTotalAmount, r.Return.CreatedAtUtc, r.Return.PostedAtUtc)).ToList(),
            page, pageSize, total);
    }

    public Task<SupplierReturnPostedResponse?> FindPostedAsync(Guid returnId, CancellationToken ct) =>
        db.Set<SupplierReturn>().AsNoTracking()
            .Where(x => x.Id == returnId && x.Status == SupplierReturnStatus.Posted)
            .Select(x => new SupplierReturnPostedResponse(
                x.Id, x.Number!, x.StockDocumentId!.Value, x.StockDocumentNumber!, x.CreditClaimTotalAmount, x.PostedAtUtc!.Value))
            .SingleOrDefaultAsync(ct);
}
