using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Application.Contracts.Terminals;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.ReadModels;

internal sealed class PosTerminalReadStore(DbContext db, PosNames names) : IPosTerminalReadStore
{
    public async Task<PosTerminalResponse?> GetAsync(Guid terminalId, CancellationToken ct)
    {
        var rows = await Rows(db.Set<PosTerminal>().AsNoTracking().Where(x => x.Id == terminalId)).ToListAsync(ct);
        return (await MapAsync(rows, ct)).SingleOrDefault();
    }

    public async Task<PagedResult<PosTerminalResponse>> ListAsync(PosTerminalFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<PosTerminal>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.IsActive is { } isActive)
            query = query.Where(x => x.IsActive == isActive);
        if (filter.Search is { } search)
            query = query.Where(x => x.Code.Contains(search) || x.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var rows = await Rows(query.OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return new PagedResult<PosTerminalResponse>(await MapAsync(rows, ct), page, pageSize, total);
    }

    private IQueryable<TerminalRow> Rows(IQueryable<PosTerminal> terminals) =>
        terminals.Select(x => new TerminalRow(x, db.Set<PosShift>()
            .Where(s => s.TerminalId == x.Id && s.Status == PosShiftStatus.Open)
            .Select(s => new OpenShift(s.Id, s.Number, s.CashierUserId))
            .FirstOrDefault()));

    private async Task<IReadOnlyList<PosTerminalResponse>> MapAsync(List<TerminalRow> rows, CancellationToken ct)
    {
        var lookup = await names.LoadAsync(
            branchIds: rows.Select(r => r.Terminal.BranchId), warehouseIds: rows.Select(r => r.Terminal.WarehouseId),
            locationIds: rows.Select(r => r.Terminal.SellingLocationId), userIds: rows.Select(r => r.Shift?.CashierUserId), ct: ct);
        return rows.Select(r => new PosTerminalResponse(
                r.Terminal.Id, r.Terminal.Code, r.Terminal.Name, r.Terminal.BranchId, lookup.BranchCode(r.Terminal.BranchId),
                r.Terminal.WarehouseId, lookup.WarehouseCode(r.Terminal.WarehouseId), r.Terminal.SellingLocationId,
                lookup.LocationCode(r.Terminal.SellingLocationId), r.Terminal.AcceptsCash, r.Terminal.AcceptsCard,
                r.Terminal.ReceiptHeader, r.Terminal.ReceiptFooter, r.Terminal.IsActive, r.Shift?.Id, r.Shift?.Number,
                r.Shift?.CashierUserId, lookup.UserName(r.Shift?.CashierUserId), Convert.ToBase64String(r.Terminal.RowVersion),
                r.Terminal.CreatedAtUtc, r.Terminal.UpdatedAtUtc))
            .ToList();
    }

    private sealed record TerminalRow(PosTerminal Terminal, OpenShift? Shift);

    private sealed record OpenShift(Guid Id, string Number, Guid CashierUserId);
}
