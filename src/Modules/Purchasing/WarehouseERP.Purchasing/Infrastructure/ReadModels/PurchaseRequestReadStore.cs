using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

internal sealed class PurchaseRequestReadStore(DbContext db, ReferenceNames names) : IPurchaseRequestReadStore
{
    public async Task<PurchaseRequestDetailsResponse?> GetAsync(Guid requestId, CancellationToken ct)
    {
        var request = await db.Set<PurchaseRequest>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Events)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == requestId, ct);
        if (request is null)
            return null;

        var lines = request.Lines.OrderBy(l => l.LineNumber).ToList();
        var lookup = await names.LoadAsync(
            productIds: lines.Select(l => l.ProductId), productUnitIds: lines.Select(l => l.ProductUnitId), warehouseIds: [request.WarehouseId],
            userIds: request.Events.Select(e => e.UserId).Append(request.CreatedByUserId).Append(request.SubmittedByUserId)
                .Append(request.ApprovedByUserId),
            ct: ct);
        var orderable = request.Status == PurchaseRequestStatus.Approved;

        return new PurchaseRequestDetailsResponse(
            request.Id, PurchasingNumbers.DraftReference(PurchasingDocumentType.PurchaseRequest, request.DraftNumber), request.Number,
            request.Status, request.WarehouseId, lookup.WarehouseCode(request.WarehouseId), request.BranchId, request.RequiresApproval,
            request.NeededByDate, request.Notes, request.ClosedReason, request.CreatedAtUtc, request.CreatedByUserId,
            lookup.UserName(request.CreatedByUserId), lookup.UserName(request.SubmittedByUserId), lookup.UserName(request.ApprovedByUserId),
            Convert.ToBase64String(request.RowVersion),
            lines.Select(l => new PurchaseRequestLineResponse(
                l.Id, l.LineNumber, l.ProductId, lookup.Sku(l.ProductId), lookup.ProductName(l.ProductId), l.ProductUnitId,
                lookup.UnitCode(l.ProductUnitId), l.Quantity, l.OrderedQuantity, orderable ? l.RemainingQuantity : 0m, l.Notes)).ToList(),
            request.Events.OrderBy(e => e.Id)
                .Select(e => new DocumentEventResponse(e.Action, e.UserId, lookup.UserName(e.UserId), e.AtUtc, e.Note)).ToList());
    }

    public async Task<PagedResult<PurchaseRequestSummaryResponse>> ListAsync(PurchaseRequestFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<PurchaseRequest>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);
        if (filter.Orderable)
            query = query.Where(x => x.Status == PurchaseRequestStatus.Approved && x.Lines.Any(l => l.OrderedQuantity < l.Quantity));
        if (filter.Search is { } search)
        {
            var draftNumber = ReferenceNames.DraftNumberOf(search);
            query = query.Where(x => (x.Number != null && x.Number.Contains(search)) || x.DraftNumber == draftNumber);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.DraftNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.DraftNumber, x.Number, x.Status, x.WarehouseId, x.NeededByDate, LineCount = x.Lines.Count, x.CreatedAtUtc,
                x.CreatedByUserId,
            })
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), userIds: rows.Select(r => r.CreatedByUserId), ct: ct);
        return new PagedResult<PurchaseRequestSummaryResponse>(
            rows.Select(r => new PurchaseRequestSummaryResponse(
                r.Id, PurchasingNumbers.DraftReference(PurchasingDocumentType.PurchaseRequest, r.DraftNumber), r.Number, r.Status,
                r.WarehouseId, lookup.WarehouseCode(r.WarehouseId), r.NeededByDate, r.LineCount, r.CreatedAtUtc,
                lookup.UserName(r.CreatedByUserId))).ToList(),
            page, pageSize, total);
    }
}
