using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

internal sealed class StockReadStore(DbContext db, ReferenceNames names) : IStockReadStore
{
    public async Task<PagedResult<StockBalanceResponse>> ListBalancesAsync(StockFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<StockBalance>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.LocationId is { } locationId)
            query = query.Where(x => x.LocationId == locationId);
        if (filter.ProductId is { } productId)
            query = query.Where(x => x.ProductId == productId);
        if (!filter.IncludeZero)
            query = query.Where(x => x.Quantity != 0);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderBy(x => x.WarehouseId).ThenBy(x => x.ProductId).ThenBy(x => x.LocationId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(
            productIds: rows.Select(r => r.ProductId), warehouseIds: rows.Select(r => r.WarehouseId),
            locationIds: rows.Select(r => r.LocationId), ct: ct);

        return new PagedResult<StockBalanceResponse>(
            rows.Select(r => new StockBalanceResponse(
                r.ProductId, lookup.Sku(r.ProductId), lookup.ProductName(r.ProductId), lookup.BaseUnitCode(r.ProductId),
                r.WarehouseId, lookup.WarehouseCode(r.WarehouseId), r.LocationId, lookup.LocationCode(r.LocationId),
                r.StockStatus, r.Quantity, r.UpdatedAtUtc))
                .ToList(),
            page, pageSize, total);
    }

    public async Task<PagedResult<StockAvailabilityResponse>> ListAvailabilityAsync(StockFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<ProductWarehouseStock>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.ProductId is { } productId)
            query = query.Where(x => x.ProductId == productId);
        if (!filter.IncludeZero)
            query = query.Where(x => x.OnHandSellable != 0 || x.ReservedQuantity != 0);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderBy(x => x.WarehouseId).ThenBy(x => x.ProductId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(productIds: rows.Select(r => r.ProductId), warehouseIds: rows.Select(r => r.WarehouseId), ct: ct);

        return new PagedResult<StockAvailabilityResponse>(
            rows.Select(r => new StockAvailabilityResponse(
                r.ProductId, lookup.Sku(r.ProductId), lookup.ProductName(r.ProductId), lookup.BaseUnitCode(r.ProductId),
                r.WarehouseId, lookup.WarehouseCode(r.WarehouseId), r.OnHandSellable, r.ReservedQuantity,
                r.OnHandSellable - r.ReservedQuantity))
                .ToList(),
            page, pageSize, total);
    }

    // The running balance starts from every earlier movement of the card: dates only limit what is shown.
    public async Task<PagedResult<StockMovementResponse>> ListMovementsAsync(StockMovementFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var card = db.Set<StockMovement>().AsNoTracking()
            .Where(x => x.ProductId == filter.ProductId && x.WarehouseId == filter.WarehouseId);
        if (filter.LocationId is { } locationId)
            card = card.Where(x => x.LocationId == locationId);
        var shown = card;
        if (filter.FromUtc is { } fromUtc)
            shown = shown.Where(x => x.PostedAtUtc >= fromUtc);
        if (filter.ToUtc is { } toUtc)
            shown = shown.Where(x => x.PostedAtUtc < toUtc);

        var total = await shown.CountAsync(ct);
        var rows = await (
                from movement in shown
                join document in db.Set<StockDocument>() on movement.DocumentId equals document.Id
                orderby movement.Id
                select new
                {
                    movement.Id, movement.PostedAtUtc, movement.DocumentId, document.Number, document.DocumentType,
                    movement.LocationId, movement.Quantity,
                })
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var balance = rows.Count == 0 ? 0m : await card.Where(x => x.Id < rows[0].Id).SumAsync(x => x.Quantity, ct);
        var lookup = await names.LoadAsync(locationIds: rows.Select(r => r.LocationId), ct: ct);

        var items = new List<StockMovementResponse>(rows.Count);
        foreach (var row in rows)
        {
            balance += row.Quantity;
            items.Add(new StockMovementResponse(
                row.Id, row.PostedAtUtc, row.DocumentId, row.Number ?? "", row.DocumentType, row.LocationId,
                lookup.LocationCode(row.LocationId), row.Quantity, balance));
        }
        return new PagedResult<StockMovementResponse>(items, page, pageSize, total);
    }

    public async Task<PagedResult<OverReservationResponse>> ListOverReservationsAsync(StockFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<OverReservationAlert>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.ProductId is { } productId)
            query = query.Where(x => x.ProductId == productId);

        var total = await query.CountAsync(ct);
        var rows = await (
                from alert in query
                join document in db.Set<StockDocument>() on alert.DocumentId equals document.Id
                orderby alert.Id descending
                select new { Alert = alert, document.Number })
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(
            productIds: rows.Select(r => r.Alert.ProductId), warehouseIds: rows.Select(r => r.Alert.WarehouseId), ct: ct);

        return new PagedResult<OverReservationResponse>(
            rows.Select(r => new OverReservationResponse(
                r.Alert.ProductId, lookup.Sku(r.Alert.ProductId), r.Alert.WarehouseId, lookup.WarehouseCode(r.Alert.WarehouseId),
                r.Alert.DocumentId, r.Number, r.Alert.OnHandSellable, r.Alert.ReservedQuantity, r.Alert.DetectedAtUtc))
                .ToList(),
            page, pageSize, total);
    }
}
