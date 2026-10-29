using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Contracts.Returns;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

internal sealed class SalesReturnReadStore(DbContext db, ReferenceNames names) : ISalesReturnReadStore
{
    public async Task<SalesReturnDetailsResponse?> GetAsync(Guid returnId, CancellationToken ct)
    {
        var salesReturn = await db.Set<SalesReturn>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == returnId, ct);
        if (salesReturn is null)
            return null;

        var customer = await db.Set<Customer>().AsNoTracking().Where(c => c.Id == salesReturn.CustomerId)
            .Select(c => new { c.Code, c.NameAr }).SingleAsync(ct);
        var deliveryNumber = await db.Set<SalesDelivery>().AsNoTracking().Where(d => d.Id == salesReturn.DeliveryId)
            .Select(d => d.Number).SingleAsync(ct);
        var orderNumber = await db.Set<SalesOrder>().AsNoTracking().Where(o => o.Id == salesReturn.OrderId).Select(o => o.Number).SingleAsync(ct);
        var lines = salesReturn.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId),
            warehouseIds: [salesReturn.WarehouseId], locationIds: lines.Select(l => l.LocationId),
            userIds: salesReturn.Events.Select(e => e.UserId)
                .Append(salesReturn.CreatedByUserId).Append(salesReturn.SubmittedByUserId).Append(salesReturn.ApprovedByUserId),
            ct: ct);

        return new SalesReturnDetailsResponse(
            salesReturn.Id, SalesNumbers.DraftReference(SalesDocumentType.Return, salesReturn.DraftNumber), salesReturn.Number,
            salesReturn.Status, salesReturn.DeliveryId, deliveryNumber, salesReturn.OrderId, orderNumber, salesReturn.CustomerId,
            customer.Code, customer.NameAr, salesReturn.WarehouseId, lookup.WarehouseCode(salesReturn.WarehouseId), salesReturn.BranchId,
            salesReturn.CurrencyCode, salesReturn.Reason, salesReturn.Notes, salesReturn.CostAmount, salesReturn.CreditNetAmount,
            salesReturn.CreditTaxAmount, salesReturn.CreditTotalAmount, salesReturn.StockDocumentId, salesReturn.StockDocumentNumber,
            salesReturn.CreatedAtUtc, salesReturn.CreatedByUserId, lookup.UserName(salesReturn.CreatedByUserId),
            lookup.UserName(salesReturn.SubmittedByUserId), lookup.UserName(salesReturn.ApprovedByUserId), salesReturn.ApprovedAtUtc,
            Convert.ToBase64String(salesReturn.RowVersion),
            lines.Select(l => new SalesReturnLineResponse(
                l.Id, l.LineNumber, l.DeliveryLineId, l.OrderLineId, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId),
                l.ProductUnitId, lookup.UnitCode(l.ProductUnitId), l.Quantity, l.Condition, l.LocationId, lookup.LocationCode(l.LocationId),
                l.StockLineId, l.CostAmount, l.UninvoicedQuantity, l.InvoicedQuantity, l.CreditNetAmount, l.CreditTaxAmount)).ToList(),
            salesReturn.Events.OrderBy(e => e.Id).Select(e => PricedLines.Event(e, lookup)).ToList());
    }

    public async Task<PagedResult<SalesReturnSummaryResponse>> ListAsync(SalesReturnFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SalesReturn>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.OrderId is { } orderId)
            query = query.Where(x => x.OrderId == orderId);
        if (filter.DeliveryId is { } deliveryId)
            query = query.Where(x => x.DeliveryId == deliveryId);
        if (filter.CustomerId is { } customerId)
            query = query.Where(x => x.CustomerId == customerId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.Search is { } search)
        {
            var draftNumber = ReferenceNames.DraftNumberOf(search);
            query = query.Where(x => (x.Number != null && x.Number.Contains(search)) || x.DraftNumber == draftNumber);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .Join(db.Set<Customer>(), r => r.CustomerId, c => c.Id, (r, c) => new { Return = r, CustomerName = c.NameAr })
            .Join(db.Set<SalesDelivery>(), x => x.Return.DeliveryId, d => d.Id, (x, d) => new { x.Return, x.CustomerName, DeliveryNumber = d.Number })
            .Join(db.Set<SalesOrder>(), x => x.Return.OrderId, o => o.Id, (x, o) => new
            {
                x.Return.Id, x.Return.DraftNumber, x.Return.Number, x.Return.Status, x.Return.DeliveryId, x.DeliveryNumber,
                x.Return.OrderId, OrderNumber = o.Number, x.Return.CustomerId, x.CustomerName, x.Return.WarehouseId,
                x.Return.CreditTotalAmount, x.Return.CreatedAtUtc, x.Return.ApprovedAtUtc,
            })
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), ct: ct);
        return new PagedResult<SalesReturnSummaryResponse>(
            rows.Select(r => new SalesReturnSummaryResponse(
                r.Id, SalesNumbers.DraftReference(SalesDocumentType.Return, r.DraftNumber), r.Number, r.Status, r.DeliveryId, r.DeliveryNumber,
                r.OrderId, r.OrderNumber, r.CustomerId, r.CustomerName, r.WarehouseId, lookup.WarehouseCode(r.WarehouseId),
                r.CreditTotalAmount, r.CreatedAtUtc, r.ApprovedAtUtc)).ToList(),
            page, pageSize, total);
    }

    public Task<SalesReturnApprovedResponse?> FindApprovedAsync(Guid returnId, CancellationToken ct) =>
        db.Set<SalesReturn>().AsNoTracking()
            .Where(x => x.Id == returnId && x.Status == SalesReturnStatus.Approved)
            .Select(x => new SalesReturnApprovedResponse(
                x.Id, x.Number!, x.StockDocumentId!.Value, x.StockDocumentNumber!, x.CostAmount, x.CreditTotalAmount, x.ApprovedAtUtc!.Value))
            .SingleOrDefaultAsync(ct);
}
