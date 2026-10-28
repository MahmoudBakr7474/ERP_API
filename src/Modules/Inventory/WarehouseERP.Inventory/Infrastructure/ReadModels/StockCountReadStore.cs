using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Contracts.Counts;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

internal sealed class StockCountReadStore(DbContext db, ReferenceNames names) : IStockCountReadStore
{
    public async Task<StockCountDetailsResponse?> GetAsync(Guid countId, CancellationToken ct)
    {
        var count = await db.Set<StockCount>().AsNoTracking()
            .Include(x => x.Scopes)
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == countId, ct);
        if (count is null)
            return null;

        var adjustment = await PostedAdjustmentAsync(countId, ct);
        var listed = count.Scopes.Where(s => s.LocationId.HasValue).Select(s => s.LocationId!.Value).ToList();
        var lookup = await names.LoadAsync(
            productIds: count.Lines.Select(l => l.ProductId),
            warehouseIds: [count.WarehouseId],
            locationIds: count.Lines.Select(l => l.LocationId).Concat(listed),
            userIds: count.Events.Select(e => e.UserId)
                .Append(count.CreatedByUserId).Append(count.StartedByUserId).Append(count.ApprovedByUserId),
            ct: ct);

        return new StockCountDetailsResponse(
            count.Id, StockDocumentNumbers.CountReference(count.DraftNumber), count.Status, count.WarehouseId,
            lookup.WarehouseCode(count.WarehouseId), count.IsWholeWarehouse,
            listed.Select(l => new StockCountLocationResponse(l, lookup.LocationCode(l))).OrderBy(l => l.LocationCode, StringComparer.Ordinal).ToList(),
            count.Notes, count.CreatedAtUtc, count.CreatedByUserId, lookup.UserName(count.CreatedByUserId), count.StartedAtUtc,
            lookup.UserName(count.StartedByUserId), count.SubmittedAtUtc, lookup.UserName(count.ApprovedByUserId), count.CompletedAtUtc,
            adjustment?.Id, adjustment?.Number, Convert.ToBase64String(count.RowVersion),
            count.Lines
                .Select(l => new StockCountLineResponse(
                    l.Id, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), lookup.BaseUnitCode(l.ProductId),
                    l.LocationId, lookup.LocationCode(l.LocationId), l.ExpectedQuantity, l.CountedQuantity, l.Variance, l.UnitCost))
                .OrderBy(l => l.LocationCode, StringComparer.Ordinal).ThenBy(l => l.Sku, StringComparer.Ordinal)
                .ToList(),
            count.Events.OrderBy(e => e.Id)
                .Select(e => new StockCountEventResponse(e.Action, e.UserId, lookup.UserName(e.UserId), e.AtUtc, e.Note))
                .ToList());
    }

    public async Task<PagedResult<StockCountSummaryResponse>> ListAsync(StockCountFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<StockCount>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.DraftNumber, x.Status, x.WarehouseId, x.IsWholeWarehouse, LineCount = x.Lines.Count,
                CountedLineCount = x.Lines.Count(l => l.CountedQuantity != null),
                DifferenceCount = x.Lines.Count(l => l.CountedQuantity != null && l.CountedQuantity != l.ExpectedQuantity),
                x.CreatedAtUtc, x.CreatedByUserId, x.StartedAtUtc, x.CompletedAtUtc,
            })
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), userIds: rows.Select(r => r.CreatedByUserId), ct: ct);

        return new PagedResult<StockCountSummaryResponse>(
            rows.Select(r => new StockCountSummaryResponse(
                r.Id, StockDocumentNumbers.CountReference(r.DraftNumber), r.Status, r.WarehouseId, lookup.WarehouseCode(r.WarehouseId),
                r.IsWholeWarehouse, r.LineCount, r.CountedLineCount, r.DifferenceCount, r.CreatedAtUtc, r.CreatedByUserId,
                lookup.UserName(r.CreatedByUserId), r.StartedAtUtc, r.CompletedAtUtc))
                .ToList(),
            page, pageSize, total);
    }

    public async Task<StockCountApprovalResponse?> FindApprovedAsync(Guid countId, CancellationToken ct)
    {
        var completedAt = await db.Set<StockCount>().AsNoTracking()
            .Where(x => x.Id == countId && x.Status == StockCountStatus.Completed)
            .Select(x => x.CompletedAtUtc)
            .SingleOrDefaultAsync(ct);
        if (completedAt is null)
            return null;

        var adjustment = await PostedAdjustmentAsync(countId, ct);
        var alerts = adjustment is null
            ? []
            : await db.Set<OverReservationAlert>().AsNoTracking().Where(a => a.DocumentId == adjustment.Id).OrderBy(a => a.Id).ToListAsync(ct);
        var lookup = await names.LoadAsync(productIds: alerts.Select(a => a.ProductId), warehouseIds: alerts.Select(a => a.WarehouseId), ct: ct);
        return new StockCountApprovalResponse(countId, adjustment?.Id, adjustment?.Number, completedAt.Value, alerts
            .Select(a => new OverReservationResponse(
                a.ProductId, lookup.Sku(a.ProductId), a.WarehouseId, lookup.WarehouseCode(a.WarehouseId), a.DocumentId,
                adjustment?.Number, a.OnHandSellable, a.ReservedQuantity, a.DetectedAtUtc))
            .ToList());
    }

    private Task<PostedAdjustment?> PostedAdjustmentAsync(Guid countId, CancellationToken ct) =>
        db.Set<StockDocument>().AsNoTracking()
            .Where(d => d.CountId == countId && d.Status == StockDocumentStatus.Posted)
            .Select(d => new PostedAdjustment(d.Id, d.Number))
            .SingleOrDefaultAsync(ct);

    private sealed record PostedAdjustment(Guid Id, string? Number);
}
