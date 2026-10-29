using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;
using WarehouseERP.Pos.Application.Contracts.CardIncidents;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.ReadModels;

internal sealed class CardIncidentReadStore(DbContext db, PosNames names) : ICardIncidentReadStore
{
    public async Task<PagedResult<PosCardIncidentResponse>> ListAsync(CardIncidentFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<PosCardIncident>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.TerminalId is { } terminalId)
            query = query.Where(x => x.TerminalId == terminalId);
        if (filter.ShiftId is { } shiftId)
            query = query.Where(x => x.ShiftId == shiftId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Join(db.Set<PosTerminal>(), i => i.TerminalId, t => t.Id, (i, t) => new { Incident = i, TerminalCode = t.Code })
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.Incident.WarehouseId),
            userIds: rows.Select(r => r.Incident.CashierUserId).Concat(rows.Select(r => r.Incident.ResolvedByUserId)), ct: ct);
        return new PagedResult<PosCardIncidentResponse>(
            rows.Select(r => new PosCardIncidentResponse(
                r.Incident.Id, r.Incident.IdempotencyKey, r.Incident.TerminalId, r.TerminalCode, r.Incident.WarehouseId,
                lookup.WarehouseCode(r.Incident.WarehouseId), r.Incident.ShiftId, r.Incident.CashierUserId,
                lookup.UserName(r.Incident.CashierUserId), r.Incident.CardAmount, r.Incident.References, r.Incident.ErrorCode,
                r.Incident.ErrorMessage, r.Incident.Status, r.Incident.Attempts, r.Incident.OccurredAtUtc, r.Incident.LastAttemptAtUtc,
                r.Incident.SaleId, r.Incident.ResolvedAtUtc, r.Incident.ResolvedByUserId, lookup.UserName(r.Incident.ResolvedByUserId),
                r.Incident.ResolutionNote, Convert.ToBase64String(r.Incident.RowVersion))).ToList(),
            page, pageSize, total);
    }
}
