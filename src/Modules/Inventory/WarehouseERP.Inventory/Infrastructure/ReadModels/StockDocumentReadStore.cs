using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

internal sealed class StockDocumentReadStore(DbContext db, ReferenceNames names) : IStockDocumentReadStore
{
    private const string DraftPrefix = "DR-";

    public async Task<StockDocumentDetailsResponse?> GetAsync(Guid documentId, CancellationToken ct)
    {
        var document = await db.Set<StockDocument>().AsNoTracking()
            .Include(x => x.Lines).ThenInclude(x => x.Allocations)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == documentId, ct);
        if (document is null)
            return null;

        var reversedBy = await db.Set<StockDocument>().AsNoTracking()
            .Where(x => x.ReversalOfDocumentId == documentId && x.Status != StockDocumentStatus.Cancelled)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
        var lines = document.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId),
            productUnitIds: lines.Select(l => l.ProductUnitId),
            warehouseIds: [document.WarehouseId],
            locationIds: lines.Select(LocationOf).Concat(lines.Select(MovedFrom).OfType<Guid>()),
            userIds: document.Events.Select(e => e.UserId)
                .Append(document.CreatedByUserId).Append(document.PostedByUserId).Append(document.ApprovedByUserId),
            ct: ct);

        return new StockDocumentDetailsResponse(
            document.Id, StockDocumentNumbers.DraftReference(document.DraftNumber), document.Number, document.DocumentType,
            document.Status, document.WarehouseId, lookup.WarehouseCode(document.WarehouseId), document.BranchId,
            document.RequiresApproval, document.ReversalOfDocumentId, reversedBy, document.TransferId, document.CountId,
            document.AdjustmentReason,
            document.ExternalReference, document.Notes, document.CreatedAtUtc, document.CreatedByUserId,
            lookup.UserName(document.CreatedByUserId), document.PostedAtUtc, lookup.UserName(document.PostedByUserId),
            lookup.UserName(document.ApprovedByUserId), Convert.ToBase64String(document.RowVersion),
            lines.Select(l => new StockDocumentLineResponse(
                l.Id, l.LineNumber, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), l.ProductUnitId,
                l.UnitId, lookup.UnitCode(l.ProductUnitId), l.ConversionFactor, l.Quantity, l.BaseQuantity,
                lookup.BaseUnitCode(l.ProductId), l.Direction, l.UnitCost, LocationOf(l), lookup.LocationCode(LocationOf(l)),
                MovedFrom(l), MovedFrom(l) is { } from ? lookup.LocationCode(from) : null))
                .ToList(),
            document.Events.OrderBy(e => e.Id)
                .Select(e => new StockDocumentEventResponse(e.Action, e.UserId, lookup.UserName(e.UserId), e.AtUtc, e.Note))
                .ToList());
    }

    public async Task<PagedResult<StockDocumentSummaryResponse>> ListAsync(
        StockDocumentFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<StockDocument>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.DocumentType is { } documentType)
            query = query.Where(x => x.DocumentType == documentType);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.Search is { } search)
        {
            var draftNumber = DraftNumberOf(search);
            query = query.Where(x => (x.Number != null && x.Number.Contains(search))
                                     || (x.ExternalReference != null && x.ExternalReference.Contains(search))
                                     || x.DraftNumber == draftNumber);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.DraftNumber, x.Number, x.DocumentType, x.Status, x.WarehouseId, x.RequiresApproval,
                x.ReversalOfDocumentId, x.ExternalReference, LineCount = x.Lines.Count, x.CreatedAtUtc, x.CreatedByUserId,
                x.PostedAtUtc,
            })
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(
            warehouseIds: rows.Select(r => r.WarehouseId), userIds: rows.Select(r => r.CreatedByUserId), ct: ct);

        return new PagedResult<StockDocumentSummaryResponse>(
            rows.Select(r => new StockDocumentSummaryResponse(
                r.Id, StockDocumentNumbers.DraftReference(r.DraftNumber), r.Number, r.DocumentType, r.Status, r.WarehouseId,
                lookup.WarehouseCode(r.WarehouseId), r.RequiresApproval, r.ReversalOfDocumentId, r.ExternalReference,
                r.LineCount, r.CreatedAtUtc, r.CreatedByUserId, lookup.UserName(r.CreatedByUserId), r.PostedAtUtc))
                .ToList(),
            page, pageSize, total);
    }

    public async Task<PostedDocumentResponse?> FindPostedAsync(Guid documentId, CancellationToken ct)
    {
        var posted = await db.Set<StockDocument>().AsNoTracking()
            .Where(x => x.Id == documentId && x.Status == StockDocumentStatus.Posted)
            .Select(x => new { x.Id, x.Number, x.PostedAtUtc })
            .SingleOrDefaultAsync(ct);
        if (posted is null)
            return null;

        var alerts = await db.Set<OverReservationAlert>().AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(
            productIds: alerts.Select(a => a.ProductId), warehouseIds: alerts.Select(a => a.WarehouseId), ct: ct);
        return new PostedDocumentResponse(posted.Id, posted.Number!, posted.PostedAtUtc!.Value, alerts
            .Select(a => new OverReservationResponse(
                a.ProductId, lookup.Sku(a.ProductId), a.WarehouseId, lookup.WarehouseCode(a.WarehouseId), a.DocumentId,
                posted.Number, a.OnHandSellable, a.ReservedQuantity, a.DetectedAtUtc))
            .ToList());
    }

    // An ordinary line has one allocation, on one side.
    private static Guid LocationOf(StockDocumentLine line) =>
        line.Allocations.Select(a => a.ToLocationId ?? a.FromLocationId).First()!.Value;

    private static Guid? MovedFrom(StockDocumentLine line) =>
        line.Direction == StockDirection.Move ? line.Allocations[0].FromLocationId : null;

    // "DR-000123", "dr-123" or "123" find draft 123.
    private static int? DraftNumberOf(string search)
    {
        var digits = search.StartsWith(DraftPrefix, StringComparison.OrdinalIgnoreCase) ? search[DraftPrefix.Length..] : search;
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;
    }
}
