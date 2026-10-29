using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Contracts.Shifts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.ReadModels;

internal sealed class PosShiftReadStore(DbContext db, PosNames names) : IPosShiftReadStore
{
    public async Task<PosShiftResponse?> GetAsync(Guid shiftId, CancellationToken ct)
    {
        var shift = await db.Set<PosShift>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == shiftId, ct);
        if (shift is null)
            return null;
        var terminal = await db.Set<PosTerminal>().AsNoTracking().Where(x => x.Id == shift.TerminalId)
            .Select(x => new { x.Code, x.Name }).SingleAsync(ct);
        var movements = await db.Set<PosCashMovement>().AsNoTracking().Where(x => x.ShiftId == shiftId).OrderBy(x => x.AtUtc).ToListAsync(ct);
        var events = await db.Set<PosShiftEvent>().AsNoTracking().Where(x => x.ShiftId == shiftId).OrderBy(x => x.Id).ToListAsync(ct);
        var openIncidents = await db.Set<PosCardIncident>().AsNoTracking()
            .CountAsync(x => x.ShiftId == shiftId && x.Status == PosCardIncidentStatus.ReversalRequired, ct);
        var lookup = await names.LoadAsync(
            branchIds: [shift.BranchId], warehouseIds: [shift.WarehouseId],
            userIds: movements.Select(m => m.UserId).Concat(events.Select(e => e.UserId)).Append(shift.CashierUserId).Append(shift.ClosedByUserId),
            company: true, ct: ct);

        return new PosShiftResponse(
            shift.Id, shift.Number, shift.Status, shift.TerminalId, terminal.Code, terminal.Name, shift.WarehouseId,
            lookup.WarehouseCode(shift.WarehouseId), shift.BranchId, lookup.BranchCode(shift.BranchId), shift.CashierUserId,
            lookup.UserName(shift.CashierUserId), lookup.CurrencyCode, shift.OpenedAtUtc, shift.OpeningFloat, shift.CashSalesAmount,
            shift.CashRefundsAmount, shift.CashInAmount, shift.CashOutAmount, shift.ExpectedCashAtClose ?? shift.ExpectedCash,
            shift.CardSalesAmount, shift.CardRefundsAmount, shift.SalesAmount, shift.RefundsAmount, shift.SalesCount, shift.ReturnsCount,
            shift.ClosedAtUtc, shift.ClosedByUserId, lookup.UserName(shift.ClosedByUserId), shift.IsForceClosed, shift.CountedCash,
            shift.CashDifference, shift.ClosingNote, openIncidents,
            movements.Select(m => new PosCashMovementResponse(m.Id, m.MovementType, m.Amount, m.Reason, m.UserId, lookup.UserName(m.UserId), m.AtUtc))
                .ToList(),
            events.Select(e => new PosShiftEventResponse(e.Id, e.Action, e.Amount, e.Note, e.UserId, lookup.UserName(e.UserId), e.AtUtc)).ToList(),
            Convert.ToBase64String(shift.RowVersion));
    }

    public Task<Guid?> FindOpenAsync(Guid terminalId, Guid cashierUserId, CancellationToken ct) =>
        db.Set<PosShift>().AsNoTracking()
            .Where(x => x.TerminalId == terminalId && x.CashierUserId == cashierUserId && x.Status == PosShiftStatus.Open)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<PagedResult<PosShiftSummaryResponse>> ListAsync(PosShiftFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<PosShift>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.TerminalId is { } terminalId)
            query = query.Where(x => x.TerminalId == terminalId);
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.CashierUserId is { } cashierUserId)
            query = query.Where(x => x.CashierUserId == cashierUserId);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.OpenedAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Join(db.Set<PosTerminal>(), s => s.TerminalId, t => t.Id, (s, t) => new { Shift = s, TerminalCode = t.Code })
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.Shift.WarehouseId),
            userIds: rows.Select(r => (Guid?)r.Shift.CashierUserId), ct: ct);
        return new PagedResult<PosShiftSummaryResponse>(
            rows.Select(r => new PosShiftSummaryResponse(
                r.Shift.Id, r.Shift.Number, r.Shift.Status, r.Shift.TerminalId, r.TerminalCode, r.Shift.WarehouseId,
                lookup.WarehouseCode(r.Shift.WarehouseId), r.Shift.CashierUserId, lookup.UserName(r.Shift.CashierUserId),
                r.Shift.OpenedAtUtc, r.Shift.ClosedAtUtc, r.Shift.OpeningFloat, r.Shift.ExpectedCashAtClose ?? r.Shift.ExpectedCash,
                r.Shift.CountedCash, r.Shift.CashDifference, r.Shift.CardSalesAmount, r.Shift.SalesCount, r.Shift.SalesAmount,
                r.Shift.IsForceClosed)).ToList(),
            page, pageSize, total);
    }
}
