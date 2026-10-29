using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Contracts.Deliveries;
using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

internal sealed class SalesDeliveryReadStore(DbContext db, ReferenceNames names) : ISalesDeliveryReadStore
{
    public async Task<SalesDeliveryDetailsResponse?> GetAsync(Guid deliveryId, CancellationToken ct)
    {
        var delivery = await db.Set<SalesDelivery>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == deliveryId, ct);
        if (delivery is null)
            return null;

        var customer = await db.Set<Customer>().AsNoTracking().Where(c => c.Id == delivery.CustomerId)
            .Select(c => new { c.Code, c.NameAr }).SingleAsync(ct);
        var order = await db.Set<SalesOrder>().AsNoTracking().Where(o => o.Id == delivery.OrderId)
            .Select(o => new { o.Number, o.ShippingAddress }).SingleAsync(ct);
        var returns = (await db.Set<SalesReturn>().AsNoTracking().Where(r => r.DeliveryId == deliveryId)
                .Select(r => new { r.Id, r.Number, r.DraftNumber, r.Status, r.CreatedAtUtc, r.CreditTotalAmount }).ToListAsync(ct))
            .Select(r => new SalesLinkedDocumentResponse(r.Id, SalesDocumentType.Return,
                r.Number ?? SalesNumbers.DraftReference(SalesDocumentType.Return, r.DraftNumber), r.Status.ToString(), r.CreatedAtUtc,
                r.CreditTotalAmount))
            .OrderBy(r => r.AtUtc)
            .ToList();

        var lines = delivery.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId),
            warehouseIds: [delivery.WarehouseId], locationIds: lines.Select(l => l.LocationId),
            userIds: delivery.Events.Select(e => e.UserId).Append(delivery.CreatedByUserId).Append(delivery.PostedByUserId), ct: ct);

        return new SalesDeliveryDetailsResponse(
            delivery.Id, SalesNumbers.DraftReference(SalesDocumentType.Delivery, delivery.DraftNumber), delivery.Number, delivery.Status,
            delivery.OrderId, order.Number, delivery.CustomerId, customer.Code, customer.NameAr, delivery.WarehouseId,
            lookup.WarehouseCode(delivery.WarehouseId), delivery.BranchId, order.ShippingAddress, delivery.Notes, delivery.CostAmount,
            delivery.StockDocumentId, delivery.StockDocumentNumber, delivery.CreatedAtUtc, lookup.UserName(delivery.CreatedByUserId),
            delivery.PostedAtUtc, lookup.UserName(delivery.PostedByUserId), Convert.ToBase64String(delivery.RowVersion),
            lines.Select(l => new SalesDeliveryLineResponse(
                l.Id, l.LineNumber, l.OrderLineId, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), l.ProductUnitId,
                lookup.UnitCode(l.ProductUnitId), l.ConversionFactor, l.Quantity, l.BaseQuantity, l.LocationId, lookup.LocationCode(l.LocationId),
                l.StockLineId, l.CostAmount, l.ReturnedQuantity)).ToList(),
            returns,
            delivery.Events.OrderBy(e => e.Id).Select(e => PricedLines.Event(e, lookup)).ToList());
    }

    public async Task<PagedResult<SalesDeliverySummaryResponse>> ListAsync(SalesDeliveryFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SalesDelivery>().AsNoTracking();
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
            query = query.Where(x => (x.Number != null && x.Number.Contains(search)) || x.DraftNumber == draftNumber);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .Join(db.Set<Customer>(), d => d.CustomerId, c => c.Id, (d, c) => new { Delivery = d, CustomerName = c.NameAr })
            .Join(db.Set<SalesOrder>(), x => x.Delivery.OrderId, o => o.Id, (x, o) => new
            {
                x.Delivery.Id, x.Delivery.DraftNumber, x.Delivery.Number, x.Delivery.Status, x.Delivery.OrderId, OrderNumber = o.Number,
                x.Delivery.CustomerId, x.CustomerName, x.Delivery.WarehouseId, x.Delivery.CreatedAtUtc, x.Delivery.PostedAtUtc,
            })
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), ct: ct);
        return new PagedResult<SalesDeliverySummaryResponse>(
            rows.Select(r => new SalesDeliverySummaryResponse(
                r.Id, SalesNumbers.DraftReference(SalesDocumentType.Delivery, r.DraftNumber), r.Number, r.Status, r.OrderId, r.OrderNumber,
                r.CustomerId, r.CustomerName, r.WarehouseId, lookup.WarehouseCode(r.WarehouseId), r.CreatedAtUtc, r.PostedAtUtc)).ToList(),
            page, pageSize, total);
    }

    public Task<SalesDeliveryPostedResponse?> FindPostedAsync(Guid deliveryId, CancellationToken ct) =>
        db.Set<SalesDelivery>().AsNoTracking()
            .Where(x => x.Id == deliveryId && x.Status == SalesDeliveryStatus.Posted)
            .Select(x => new SalesDeliveryPostedResponse(
                x.Id, x.Number!, x.StockDocumentId!.Value, x.StockDocumentNumber!, x.CostAmount, x.PostedAtUtc!.Value))
            .SingleOrDefaultAsync(ct);
}
