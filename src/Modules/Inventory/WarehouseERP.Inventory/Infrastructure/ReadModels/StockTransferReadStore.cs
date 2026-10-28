using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

internal sealed class StockTransferReadStore(DbContext db, ReferenceNames names) : IStockTransferReadStore
{
    private const string ReferencePrefix = "TR-";

    public async Task<StockTransferDetailsResponse?> GetAsync(Guid transferId, CancellationToken ct)
    {
        var transfer = await db.Set<StockTransfer>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == transferId, ct);
        if (transfer is null)
            return null;

        var documents = await db.Set<StockDocument>().AsNoTracking()
            .Where(d => d.TransferId == transferId)
            .OrderBy(d => d.CreatedAtUtc).ThenBy(d => d.DraftNumber)
            .Select(d => new StockTransferDocumentResponse(
                d.Id, d.DocumentType, d.Status, StockDocumentNumbers.DraftReference(d.DraftNumber), d.Number, d.CreatedAtUtc, d.PostedAtUtc))
            .ToListAsync(ct);
        var lines = transfer.Lines.OrderBy(l => l.LineNumber).ToList();
        var locationIds = lines.Select(l => l.SourceLocationId).ToList();
        if (transfer.TransitLocationId is { } transitLocationId)
            locationIds.Add(transitLocationId);
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId),
            productUnitIds: lines.Select(l => l.ProductUnitId),
            warehouseIds: [transfer.SourceWarehouseId, transfer.DestinationWarehouseId, transfer.TransitWarehouseId],
            locationIds: locationIds,
            userIds: transfer.Events.Select(e => e.UserId)
                .Append(transfer.CreatedByUserId).Append(transfer.DispatchedByUserId).Append(transfer.ApprovedByUserId),
            ct: ct);

        return new StockTransferDetailsResponse(
            transfer.Id, StockDocumentNumbers.TransferReference(transfer.DraftNumber), transfer.Number, transfer.Status,
            transfer.SourceWarehouseId, lookup.WarehouseCode(transfer.SourceWarehouseId),
            transfer.DestinationWarehouseId, lookup.WarehouseCode(transfer.DestinationWarehouseId),
            transfer.TransitWarehouseId, lookup.WarehouseCode(transfer.TransitWarehouseId),
            transfer.TransitLocationId, transfer.TransitLocationId is { } transit ? lookup.LocationCode(transit) : null,
            transfer.RequiresApproval, transfer.ExternalReference, transfer.Notes, transfer.CreatedAtUtc, transfer.CreatedByUserId,
            lookup.UserName(transfer.CreatedByUserId), transfer.DispatchedAtUtc, lookup.UserName(transfer.DispatchedByUserId),
            lookup.UserName(transfer.ApprovedByUserId), transfer.CompletedAtUtc, transfer.OutstandingQuantity,
            Convert.ToBase64String(transfer.RowVersion),
            lines.Select(l => new StockTransferLineResponse(
                l.Id, l.LineNumber, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), l.ProductUnitId,
                lookup.UnitCode(l.ProductUnitId), l.ConversionFactor, l.Quantity, l.BaseQuantity, lookup.BaseUnitCode(l.ProductId),
                l.SourceLocationId, lookup.LocationCode(l.SourceLocationId), l.ReceivedQuantity, l.LostQuantity, l.OutstandingQuantity))
                .ToList(),
            documents,
            transfer.Events.OrderBy(e => e.Id)
                .Select(e => new StockTransferEventResponse(e.Action, e.UserId, lookup.UserName(e.UserId), e.AtUtc, e.Note))
                .ToList());
    }

    public async Task<PagedResult<StockTransferSummaryResponse>> ListAsync(
        StockTransferFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<StockTransfer>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.SourceWarehouseId) || ids.Contains(x.DestinationWarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.SourceWarehouseId == warehouseId || x.DestinationWarehouseId == warehouseId);
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
                x.Id, x.DraftNumber, x.Number, x.Status, x.SourceWarehouseId, x.DestinationWarehouseId, x.TransitWarehouseId,
                x.RequiresApproval, LineCount = x.Lines.Count,
                Outstanding = x.Lines.Sum(l => l.BaseQuantity - l.ReceivedQuantity - l.LostQuantity),
                x.CreatedAtUtc, x.CreatedByUserId, x.DispatchedAtUtc, x.CompletedAtUtc,
            })
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(
            warehouseIds: rows.SelectMany(r => new[] { r.SourceWarehouseId, r.DestinationWarehouseId, r.TransitWarehouseId }),
            userIds: rows.Select(r => r.CreatedByUserId), ct: ct);

        return new PagedResult<StockTransferSummaryResponse>(
            rows.Select(r => new StockTransferSummaryResponse(
                r.Id, StockDocumentNumbers.TransferReference(r.DraftNumber), r.Number, r.Status, r.SourceWarehouseId,
                lookup.WarehouseCode(r.SourceWarehouseId), r.DestinationWarehouseId, lookup.WarehouseCode(r.DestinationWarehouseId),
                r.TransitWarehouseId, lookup.WarehouseCode(r.TransitWarehouseId), r.RequiresApproval, r.LineCount,
                r.Status is StockTransferStatus.Draft or StockTransferStatus.Submitted or StockTransferStatus.Cancelled ? 0m : r.Outstanding,
                r.CreatedAtUtc, r.CreatedByUserId, lookup.UserName(r.CreatedByUserId), r.DispatchedAtUtc, r.CompletedAtUtc))
                .ToList(),
            page, pageSize, total);
    }

    public async Task<StockTransferPostedResponse?> FindPostedAsync(Guid transferId, Guid documentId, CancellationToken ct)
    {
        var posted = await (
                from document in db.Set<StockDocument>().AsNoTracking()
                where document.Id == documentId && document.TransferId == transferId && document.Status == StockDocumentStatus.Posted
                join transfer in db.Set<StockTransfer>().AsNoTracking() on document.TransferId equals transfer.Id
                select new { document.Number, document.PostedAtUtc, transfer.Status })
            .SingleOrDefaultAsync(ct);
        if (posted is null)
            return null;

        var alerts = await db.Set<OverReservationAlert>().AsNoTracking().Where(a => a.DocumentId == documentId).OrderBy(a => a.Id).ToListAsync(ct);
        var lookup = await names.LoadAsync(productIds: alerts.Select(a => a.ProductId), warehouseIds: alerts.Select(a => a.WarehouseId), ct: ct);
        return new StockTransferPostedResponse(transferId, documentId, posted.Number!, posted.Status, posted.PostedAtUtc!.Value, alerts
            .Select(a => new OverReservationResponse(
                a.ProductId, lookup.Sku(a.ProductId), a.WarehouseId, lookup.WarehouseCode(a.WarehouseId), a.DocumentId, posted.Number,
                a.OnHandSellable, a.ReservedQuantity, a.DetectedAtUtc))
            .ToList());
    }

    // "TR-000123", "tr-123" or "123" find transfer 123.
    private static int? DraftNumberOf(string search)
    {
        var digits = search.StartsWith(ReferencePrefix, StringComparison.OrdinalIgnoreCase) ? search[ReferencePrefix.Length..] : search;
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;
    }
}
