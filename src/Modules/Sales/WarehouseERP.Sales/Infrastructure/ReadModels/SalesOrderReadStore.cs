using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Application.Contracts.Orders;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

internal sealed class SalesOrderReadStore(DbContext db, ReferenceNames names) : ISalesOrderReadStore
{
    public async Task<SalesOrderDetailsResponse?> GetAsync(Guid orderId, CancellationToken ct)
    {
        var order = await db.Set<SalesOrder>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == orderId, ct);
        if (order is null)
            return null;

        var customer = await db.Set<Customer>().AsNoTracking().Where(c => c.Id == order.CustomerId)
            .Select(c => new { c.Code, c.NameAr }).SingleAsync(ct);
        var quotationNumber = order.SourceQuotationId is { } quotationId
            ? await db.Set<SalesQuotation>().AsNoTracking().Where(q => q.Id == quotationId).Select(q => q.Number).SingleOrDefaultAsync(ct)
            : null;
        var deliveries = (await db.Set<SalesDelivery>().AsNoTracking().Where(d => d.OrderId == orderId)
                .Select(d => new { d.Id, d.Number, d.DraftNumber, d.Status, d.CreatedAtUtc, d.CostAmount }).ToListAsync(ct))
            .Select(d => new SalesLinkedDocumentResponse(d.Id, SalesDocumentType.Delivery,
                d.Number ?? SalesNumbers.DraftReference(SalesDocumentType.Delivery, d.DraftNumber), d.Status.ToString(), d.CreatedAtUtc,
                d.CostAmount));
        var invoices = (await db.Set<SalesInvoice>().AsNoTracking().Where(i => i.OrderId == orderId)
                .Select(i => new { i.Id, i.Number, i.DraftNumber, i.Status, i.CreatedAtUtc, i.TotalAmount }).ToListAsync(ct))
            .Select(i => new SalesLinkedDocumentResponse(i.Id, SalesDocumentType.Invoice,
                i.Number ?? SalesNumbers.DraftReference(SalesDocumentType.Invoice, i.DraftNumber), i.Status.ToString(), i.CreatedAtUtc,
                i.TotalAmount));
        var returns = (await db.Set<SalesReturn>().AsNoTracking().Where(r => r.OrderId == orderId)
                .Select(r => new { r.Id, r.Number, r.DraftNumber, r.Status, r.CreatedAtUtc, r.CreditTotalAmount }).ToListAsync(ct))
            .Select(r => new SalesLinkedDocumentResponse(r.Id, SalesDocumentType.Return,
                r.Number ?? SalesNumbers.DraftReference(SalesDocumentType.Return, r.DraftNumber), r.Status.ToString(), r.CreatedAtUtc,
                r.CreditTotalAmount));

        var lines = order.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId), warehouseIds: [order.WarehouseId],
            userIds: order.Events.Select(e => e.UserId).Append(order.CreatedByUserId).Append(order.SubmittedByUserId).Append(order.ApprovedByUserId),
            ct: ct);
        var open = order.IsOpen;

        return new SalesOrderDetailsResponse(
            order.Id, SalesNumbers.DraftReference(SalesDocumentType.Order, order.DraftNumber), order.Number, order.Status, order.CustomerId,
            customer.Code, customer.NameAr, order.WarehouseId, lookup.WarehouseCode(order.WarehouseId), order.BranchId, order.CurrencyCode,
            order.OrderDate, order.RequestedDeliveryDate, order.CustomerReference, order.PaymentTermDays, order.ShippingAddress, order.Notes,
            order.SourceQuotationId, quotationNumber, order.GrossAmount, order.DiscountAmount, order.NetAmount, order.TaxAmount,
            order.TotalAmount, order.InvoicedAmount, order.CreditedAmount, ApprovalReasons.FromFlags(order.ApprovalReasonFlags),
            order.ClosedReason, order.CreatedAtUtc, order.CreatedByUserId, lookup.UserName(order.CreatedByUserId),
            lookup.UserName(order.SubmittedByUserId), lookup.UserName(order.ApprovedByUserId), order.ApprovedAtUtc, order.ConfirmedAtUtc,
            Convert.ToBase64String(order.RowVersion),
            lines.Select(l => new SalesOrderLineResponse(
                l.Id, l.LineNumber, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), l.ProductUnitId,
                lookup.UnitCode(l.ProductUnitId), l.ConversionFactor, l.Quantity, l.UnitPrice, l.PriceSource, l.ListUnitPrice,
                l.DiscountPercent, l.TaxTreatment, l.TaxRate, l.GrossAmount, l.DiscountAmount, l.NetAmount, l.TaxAmount, l.TotalAmount,
                l.NetUnitPrice, l.DeliveredQuantity, l.ClosedQuantity, open ? l.RemainingQuantity : 0m, l.InvoicedQuantity,
                l.InvoiceableQuantity, l.ReturnedQuantity, l.ReturnedInvoicedQuantity)).ToList(),
            deliveries.Concat(invoices).Concat(returns).OrderBy(d => d.AtUtc).ToList(),
            order.Events.OrderBy(e => e.Id).Select(e => PricedLines.Event(e, lookup)).ToList());
    }

    public async Task<PagedResult<SalesOrderSummaryResponse>> ListAsync(SalesOrderFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SalesOrder>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
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
            .Join(db.Set<Customer>(), o => o.CustomerId, c => c.Id, (o, c) => new
            {
                o.Id, o.DraftNumber, o.Number, o.Status, o.CustomerId, CustomerCode = c.Code, CustomerName = c.NameAr, o.WarehouseId,
                o.CurrencyCode, o.TotalAmount, o.InvoicedAmount, o.OrderDate, o.RequestedDeliveryDate, o.CreatedAtUtc, o.CreatedByUserId,
                o.ConfirmedAtUtc,
            })
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), userIds: rows.Select(r => r.CreatedByUserId), ct: ct);
        return new PagedResult<SalesOrderSummaryResponse>(
            rows.Select(r => new SalesOrderSummaryResponse(
                r.Id, SalesNumbers.DraftReference(SalesDocumentType.Order, r.DraftNumber), r.Number, r.Status, r.CustomerId, r.CustomerCode,
                r.CustomerName, r.WarehouseId, lookup.WarehouseCode(r.WarehouseId), r.CurrencyCode, r.TotalAmount, r.InvoicedAmount,
                r.OrderDate, r.RequestedDeliveryDate, r.CreatedAtUtc, lookup.UserName(r.CreatedByUserId), r.ConfirmedAtUtc)).ToList(),
            page, pageSize, total);
    }
}
