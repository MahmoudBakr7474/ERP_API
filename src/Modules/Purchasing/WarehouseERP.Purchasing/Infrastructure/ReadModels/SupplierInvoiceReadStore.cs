using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

internal sealed class SupplierInvoiceReadStore(DbContext db, ReferenceNames names) : ISupplierInvoiceReadStore
{
    public async Task<SupplierInvoiceDetailsResponse?> GetAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.Set<SupplierInvoice>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == invoiceId, ct);
        if (invoice is null)
            return null;

        var orderNumber = await db.Set<PurchaseOrder>().AsNoTracking().Where(o => o.Id == invoice.PurchaseOrderId).Select(o => o.Number).SingleAsync(ct);
        var supplier = await db.Set<Supplier>().AsNoTracking().Where(s => s.Id == invoice.SupplierId)
            .Select(s => new { s.Code, s.NameAr }).SingleAsync(ct);
        var lines = invoice.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId), warehouseIds: [invoice.WarehouseId],
            userIds: invoice.Events.Select(e => e.UserId).Append(invoice.CreatedByUserId).Append(invoice.SubmittedByUserId)
                .Append(invoice.ApprovedByUserId).Append(invoice.PostedByUserId),
            ct: ct);

        return new SupplierInvoiceDetailsResponse(
            invoice.Id, PurchasingNumbers.DraftReference(PurchasingDocumentType.SupplierInvoice, invoice.DraftNumber), invoice.Number,
            invoice.Status, invoice.SupplierId, supplier.Code, supplier.NameAr, invoice.PurchaseOrderId, orderNumber ?? "", invoice.WarehouseId,
            lookup.WarehouseCode(invoice.WarehouseId), invoice.BranchId, invoice.CurrencyCode, invoice.SupplierInvoiceNumber, invoice.InvoiceDate,
            invoice.DueDate, invoice.Notes, invoice.RequiresApproval, invoice.GrossAmount, invoice.DiscountAmount, invoice.NetAmount,
            invoice.TaxAmount, invoice.TotalAmount, invoice.PriceVarianceAmount, invoice.CreatedAtUtc, invoice.CreatedByUserId,
            lookup.UserName(invoice.CreatedByUserId), lookup.UserName(invoice.SubmittedByUserId), lookup.UserName(invoice.ApprovedByUserId),
            invoice.PostedAtUtc, lookup.UserName(invoice.PostedByUserId), Convert.ToBase64String(invoice.RowVersion),
            lines.Select(l => new SupplierInvoiceLineResponse(
                l.Id, l.LineNumber, l.PurchaseOrderLineId, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId),
                l.ProductUnitId, lookup.UnitCode(l.ProductUnitId), l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxRate, l.TaxTreatment,
                l.GrossAmount, l.DiscountAmount, l.NetAmount, l.TaxAmount, l.TotalAmount, l.NetUnitPrice, l.OrderNetUnitPrice,
                l.PriceVarianceAmount)).ToList(),
            invoice.Events.OrderBy(e => e.Id)
                .Select(e => new DocumentEventResponse(e.Action, e.UserId, lookup.UserName(e.UserId), e.AtUtc, e.Note)).ToList());
    }

    public async Task<PagedResult<SupplierInvoiceSummaryResponse>> ListAsync(SupplierInvoiceFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SupplierInvoice>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.SupplierId is { } supplierId)
            query = query.Where(x => x.SupplierId == supplierId);
        if (filter.PurchaseOrderId is { } orderId)
            query = query.Where(x => x.PurchaseOrderId == orderId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.DueOnOrBefore is { } due)
            query = query.Where(x => x.Status == SupplierInvoiceStatus.Posted && x.DueDate <= due);
        if (filter.Search is { } search)
        {
            var draftNumber = ReferenceNames.DraftNumberOf(search);
            query = query.Where(x => x.SupplierInvoiceNumber.Contains(search) || (x.Number != null && x.Number.Contains(search))
                                     || x.DraftNumber == draftNumber);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .Join(db.Set<PurchaseOrder>(), i => i.PurchaseOrderId, o => o.Id, (i, o) => new { Invoice = i, OrderNumber = o.Number })
            .Join(db.Set<Supplier>(), x => x.Invoice.SupplierId, s => s.Id, (x, s) => new { x.Invoice, x.OrderNumber, SupplierName = s.NameAr })
            .OrderByDescending(x => x.Invoice.CreatedAtUtc).ThenByDescending(x => x.Invoice.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.Invoice.WarehouseId), ct: ct);
        return new PagedResult<SupplierInvoiceSummaryResponse>(
            rows.Select(r => new SupplierInvoiceSummaryResponse(
                r.Invoice.Id, PurchasingNumbers.DraftReference(PurchasingDocumentType.SupplierInvoice, r.Invoice.DraftNumber), r.Invoice.Number,
                r.Invoice.Status, r.Invoice.SupplierId, r.SupplierName, r.Invoice.PurchaseOrderId, r.OrderNumber ?? "", r.Invoice.WarehouseId,
                lookup.WarehouseCode(r.Invoice.WarehouseId), r.Invoice.SupplierInvoiceNumber, r.Invoice.InvoiceDate, r.Invoice.DueDate,
                r.Invoice.CurrencyCode, r.Invoice.TotalAmount, r.Invoice.PriceVarianceAmount, r.Invoice.RequiresApproval,
                r.Invoice.CreatedAtUtc, r.Invoice.PostedAtUtc)).ToList(),
            page, pageSize, total);
    }

    public Task<SupplierInvoicePostedResponse?> FindPostedAsync(Guid invoiceId, CancellationToken ct) =>
        db.Set<SupplierInvoice>().AsNoTracking()
            .Where(x => x.Id == invoiceId && x.Status == SupplierInvoiceStatus.Posted)
            .Select(x => new SupplierInvoicePostedResponse(x.Id, x.Number!, x.TotalAmount, x.PriceVarianceAmount, x.DueDate, x.PostedAtUtc!.Value))
            .SingleOrDefaultAsync(ct);
}
