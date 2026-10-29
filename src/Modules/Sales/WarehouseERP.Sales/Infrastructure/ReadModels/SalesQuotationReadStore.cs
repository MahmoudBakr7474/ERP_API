using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Contracts.Quotations;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

internal sealed class SalesQuotationReadStore(DbContext db, ReferenceNames names) : ISalesQuotationReadStore
{
    public async Task<SalesQuotationDetailsResponse?> GetAsync(Guid quotationId, DateOnly today, CancellationToken ct)
    {
        var quotation = await db.Set<SalesQuotation>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == quotationId, ct);
        if (quotation is null)
            return null;

        var customer = await db.Set<Customer>().AsNoTracking().Where(c => c.Id == quotation.CustomerId)
            .Select(c => new { c.Code, c.NameAr }).SingleAsync(ct);
        var lines = quotation.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId), warehouseIds: [quotation.WarehouseId],
            userIds: quotation.Events.Select(e => e.UserId)
                .Append(quotation.CreatedByUserId).Append(quotation.SubmittedByUserId).Append(quotation.ApprovedByUserId),
            ct: ct);

        return new SalesQuotationDetailsResponse(
            quotation.Id, SalesNumbers.DraftReference(SalesDocumentType.Quotation, quotation.DraftNumber), quotation.Number, quotation.Status,
            quotation.IsExpiredOn(today), quotation.CustomerId, customer.Code, customer.NameAr, quotation.WarehouseId,
            lookup.WarehouseCode(quotation.WarehouseId), quotation.BranchId, quotation.CurrencyCode, quotation.QuotationDate,
            quotation.ValidUntil, quotation.CustomerReference, quotation.PaymentTermDays, quotation.Notes, quotation.GrossAmount,
            quotation.DiscountAmount, quotation.NetAmount, quotation.TaxAmount, quotation.TotalAmount,
            ApprovalReasons.FromFlags(quotation.ApprovalReasonFlags), quotation.DeclinedReason, quotation.ConvertedOrderId,
            quotation.CreatedAtUtc, quotation.CreatedByUserId, lookup.UserName(quotation.CreatedByUserId),
            lookup.UserName(quotation.SubmittedByUserId), lookup.UserName(quotation.ApprovedByUserId), quotation.ApprovedAtUtc,
            quotation.IssuedAtUtc, Convert.ToBase64String(quotation.RowVersion),
            lines.Select(l => PricedLines.Response(l, lookup)).ToList(),
            quotation.Events.OrderBy(e => e.Id).Select(e => PricedLines.Event(e, lookup)).ToList());
    }

    public async Task<PagedResult<SalesQuotationSummaryResponse>> ListAsync(
        SalesQuotationFilter filter, DateOnly today, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SalesQuotation>().AsNoTracking();
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
            .Join(db.Set<Customer>(), q => q.CustomerId, c => c.Id, (q, c) => new
            {
                q.Id, q.DraftNumber, q.Number, q.Status, q.CustomerId, CustomerCode = c.Code, CustomerName = c.NameAr, q.WarehouseId,
                q.CurrencyCode, q.TotalAmount, q.QuotationDate, q.ValidUntil, q.CreatedAtUtc, q.CreatedByUserId,
            })
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), userIds: rows.Select(r => r.CreatedByUserId), ct: ct);
        return new PagedResult<SalesQuotationSummaryResponse>(
            rows.Select(r => new SalesQuotationSummaryResponse(
                r.Id, SalesNumbers.DraftReference(SalesDocumentType.Quotation, r.DraftNumber), r.Number, r.Status,
                r.Status == SalesQuotationStatus.Issued && r.ValidUntil < today, r.CustomerId, r.CustomerCode, r.CustomerName,
                r.WarehouseId, lookup.WarehouseCode(r.WarehouseId), r.CurrencyCode, r.TotalAmount, r.QuotationDate, r.ValidUntil,
                r.CreatedAtUtc, lookup.UserName(r.CreatedByUserId))).ToList(),
            page, pageSize, total);
    }
}
