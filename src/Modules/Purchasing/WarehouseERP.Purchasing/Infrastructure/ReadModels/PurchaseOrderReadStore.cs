using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

internal sealed class PurchaseOrderReadStore(DbContext db, ReferenceNames names) : IPurchaseOrderReadStore
{
    public async Task<PurchaseOrderDetailsResponse?> GetAsync(Guid orderId, CancellationToken ct)
    {
        var order = await db.Set<PurchaseOrder>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == orderId, ct);
        if (order is null)
            return null;

        var supplier = await db.Set<Supplier>().AsNoTracking().Where(s => s.Id == order.SupplierId)
            .Select(s => new { s.Code, s.NameAr }).SingleAsync(ct);
        var receipts = await db.Set<GoodsReceipt>().AsNoTracking().Where(r => r.PurchaseOrderId == orderId)
            .Select(r => new LinkedDocumentResponse(r.Id, PurchasingDocumentType.GoodsReceipt, r.Number, "Posted", r.ReceivedAtUtc, null))
            .ToListAsync(ct);
        var invoices = (await db.Set<SupplierInvoice>().AsNoTracking().Where(i => i.PurchaseOrderId == orderId)
                .Select(i => new { i.Id, i.Number, i.DraftNumber, i.Status, i.CreatedAtUtc, i.TotalAmount }).ToListAsync(ct))
            .Select(i => new LinkedDocumentResponse(i.Id, PurchasingDocumentType.SupplierInvoice,
                i.Number ?? PurchasingNumbers.DraftReference(PurchasingDocumentType.SupplierInvoice, i.DraftNumber), i.Status.ToString(),
                i.CreatedAtUtc, i.TotalAmount));
        var returns = (await db.Set<SupplierReturn>().AsNoTracking().Where(r => r.PurchaseOrderId == orderId)
                .Select(r => new { r.Id, r.Number, r.DraftNumber, r.Status, r.CreatedAtUtc, r.CreditClaimTotalAmount }).ToListAsync(ct))
            .Select(r => new LinkedDocumentResponse(r.Id, PurchasingDocumentType.SupplierReturn,
                r.Number ?? PurchasingNumbers.DraftReference(PurchasingDocumentType.SupplierReturn, r.DraftNumber), r.Status.ToString(),
                r.CreatedAtUtc, r.CreditClaimTotalAmount));

        var lines = order.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId), warehouseIds: [order.WarehouseId],
            userIds: order.Events.Select(e => e.UserId).Append(order.CreatedByUserId).Append(order.SubmittedByUserId).Append(order.ApprovedByUserId),
            ct: ct);
        var open = order.Status is PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived;

        return new PurchaseOrderDetailsResponse(
            order.Id, PurchasingNumbers.DraftReference(PurchasingDocumentType.PurchaseOrder, order.DraftNumber), order.Number, order.Status,
            order.SupplierId, supplier.Code, supplier.NameAr, order.WarehouseId, lookup.WarehouseCode(order.WarehouseId), order.BranchId,
            order.CurrencyCode, order.RequiresApproval, order.ExpectedDeliveryDate, order.SupplierReference, order.PaymentTermDays,
            order.Notes, order.GrossAmount, order.DiscountAmount, order.NetAmount, order.TaxAmount, order.TotalAmount, order.ClosedReason,
            order.CreatedAtUtc, order.CreatedByUserId, lookup.UserName(order.CreatedByUserId), lookup.UserName(order.SubmittedByUserId),
            lookup.UserName(order.ApprovedByUserId), order.ApprovedAtUtc, Convert.ToBase64String(order.RowVersion),
            lines.Select(l => new PurchaseOrderLineResponse(
                l.Id, l.LineNumber, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), l.ProductUnitId,
                lookup.UnitCode(l.ProductUnitId), l.ConversionFactor, l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxRate, l.TaxTreatment,
                l.GrossAmount, l.DiscountAmount, l.NetAmount, l.TaxAmount, l.TotalAmount, l.NetUnitPrice, l.PurchaseRequestLineId,
                l.ReceivedQuantity, open ? l.RemainingQuantity : 0m, l.InvoicedQuantity, l.InvoiceableQuantity, l.ReturnedQuantity,
                l.ReturnedInvoicedQuantity)).ToList(),
            receipts.Concat(invoices).Concat(returns).OrderBy(d => d.AtUtc).ToList(),
            order.Events.OrderBy(e => e.Id)
                .Select(e => new DocumentEventResponse(e.Action, e.UserId, lookup.UserName(e.UserId), e.AtUtc, e.Note)).ToList());
    }

    public async Task<PagedResult<PurchaseOrderSummaryResponse>> ListAsync(PurchaseOrderFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<PurchaseOrder>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.SupplierId is { } supplierId)
            query = query.Where(x => x.SupplierId == supplierId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.Search is { } search)
        {
            var draftNumber = ReferenceNames.DraftNumberOf(search);
            query = query.Where(x => (x.Number != null && x.Number.Contains(search))
                                     || (x.SupplierReference != null && x.SupplierReference.Contains(search))
                                     || x.DraftNumber == draftNumber);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .Join(db.Set<Supplier>(), o => o.SupplierId, s => s.Id, (o, s) => new
            {
                o.Id, o.DraftNumber, o.Number, o.Status, o.SupplierId, SupplierCode = s.Code, SupplierName = s.NameAr, o.WarehouseId,
                o.CurrencyCode, o.TotalAmount, o.ExpectedDeliveryDate, o.CreatedAtUtc, o.CreatedByUserId, o.ApprovedAtUtc,
            })
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), userIds: rows.Select(r => r.CreatedByUserId), ct: ct);
        return new PagedResult<PurchaseOrderSummaryResponse>(
            rows.Select(r => new PurchaseOrderSummaryResponse(
                r.Id, PurchasingNumbers.DraftReference(PurchasingDocumentType.PurchaseOrder, r.DraftNumber), r.Number, r.Status,
                r.SupplierId, r.SupplierCode, r.SupplierName, r.WarehouseId, lookup.WarehouseCode(r.WarehouseId), r.CurrencyCode,
                r.TotalAmount, r.ExpectedDeliveryDate, r.CreatedAtUtc, lookup.UserName(r.CreatedByUserId), r.ApprovedAtUtc)).ToList(),
            page, pageSize, total);
    }
}
