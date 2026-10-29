using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Contracts.Invoices;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

internal sealed class SalesInvoiceReadStore(DbContext db, ReferenceNames names) : ISalesInvoiceReadStore
{
    public async Task<SalesInvoiceDetailsResponse?> GetAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.Set<SalesInvoice>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == invoiceId, ct);
        if (invoice is null)
            return null;

        var customer = await db.Set<Customer>().AsNoTracking().Where(c => c.Id == invoice.CustomerId)
            .Select(c => new { c.Code, c.NameAr, c.TaxRegistrationNumber }).SingleAsync(ct);
        var orderNumber = await db.Set<SalesOrder>().AsNoTracking().Where(o => o.Id == invoice.OrderId).Select(o => o.Number).SingleAsync(ct);
        var lines = invoice.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId), warehouseIds: [invoice.WarehouseId],
            userIds: invoice.Events.Select(e => e.UserId).Append(invoice.CreatedByUserId).Append(invoice.PostedByUserId), ct: ct);

        return new SalesInvoiceDetailsResponse(
            invoice.Id, SalesNumbers.DraftReference(SalesDocumentType.Invoice, invoice.DraftNumber), invoice.Number, invoice.Status,
            invoice.OrderId, orderNumber, invoice.CustomerId, customer.Code, customer.NameAr, customer.TaxRegistrationNumber,
            invoice.WarehouseId, lookup.WarehouseCode(invoice.WarehouseId), invoice.BranchId, invoice.CurrencyCode, invoice.InvoiceDate,
            invoice.PaymentTermDays, invoice.DueDate, invoice.CustomerReference, invoice.Notes, invoice.GrossAmount, invoice.DiscountAmount,
            invoice.NetAmount, invoice.TaxAmount, invoice.TotalAmount, invoice.CreatedAtUtc, lookup.UserName(invoice.CreatedByUserId),
            invoice.PostedAtUtc, lookup.UserName(invoice.PostedByUserId), Convert.ToBase64String(invoice.RowVersion),
            lines.Select(l => new SalesInvoiceLineResponse(
                l.Id, l.LineNumber, l.OrderLineId, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), l.ProductUnitId,
                lookup.UnitCode(l.ProductUnitId), l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxTreatment, l.TaxRate, l.GrossAmount,
                l.DiscountAmount, l.NetAmount, l.TaxAmount, l.TotalAmount)).ToList(),
            invoice.Events.OrderBy(e => e.Id).Select(e => PricedLines.Event(e, lookup)).ToList());
    }

    public async Task<PagedResult<SalesInvoiceSummaryResponse>> ListAsync(SalesInvoiceFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SalesInvoice>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.OrderId is { } orderId)
            query = query.Where(x => x.OrderId == orderId);
        if (filter.CustomerId is { } customerId)
            query = query.Where(x => x.CustomerId == customerId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.Search is { } search)
        {
            var draftNumber = ReferenceNames.DraftNumberOf(search);
            query = query.Where(x => (x.Number != null && x.Number.Contains(search))
                                     || (x.CustomerReference != null && x.CustomerReference.Contains(search))
                                     || x.DraftNumber == draftNumber);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .Join(db.Set<Customer>(), i => i.CustomerId, c => c.Id, (i, c) => new { Invoice = i, CustomerName = c.NameAr })
            .Join(db.Set<SalesOrder>(), x => x.Invoice.OrderId, o => o.Id, (x, o) => new
            {
                x.Invoice.Id, x.Invoice.DraftNumber, x.Invoice.Number, x.Invoice.Status, x.Invoice.OrderId, OrderNumber = o.Number,
                x.Invoice.CustomerId, x.CustomerName, x.Invoice.WarehouseId, x.Invoice.CurrencyCode, x.Invoice.InvoiceDate,
                x.Invoice.DueDate, x.Invoice.TotalAmount, x.Invoice.CreatedAtUtc, x.Invoice.PostedAtUtc,
            })
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), ct: ct);
        return new PagedResult<SalesInvoiceSummaryResponse>(
            rows.Select(r => new SalesInvoiceSummaryResponse(
                r.Id, SalesNumbers.DraftReference(SalesDocumentType.Invoice, r.DraftNumber), r.Number, r.Status, r.OrderId, r.OrderNumber,
                r.CustomerId, r.CustomerName, r.WarehouseId, lookup.WarehouseCode(r.WarehouseId), r.CurrencyCode, r.InvoiceDate, r.DueDate,
                r.TotalAmount, r.CreatedAtUtc, r.PostedAtUtc)).ToList(),
            page, pageSize, total);
    }

    public Task<SalesInvoicePostedResponse?> FindPostedAsync(Guid invoiceId, CancellationToken ct) =>
        db.Set<SalesInvoice>().AsNoTracking()
            .Where(x => x.Id == invoiceId && x.Status == SalesInvoiceStatus.Posted)
            .Select(x => new SalesInvoicePostedResponse(x.Id, x.Number!, x.TotalAmount, x.DueDate, x.PostedAtUtc!.Value))
            .SingleOrDefaultAsync(ct);
}
