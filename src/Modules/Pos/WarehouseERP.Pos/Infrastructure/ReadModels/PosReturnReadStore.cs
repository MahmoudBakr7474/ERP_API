using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.ReadModels;

internal sealed class PosReturnReadStore(DbContext db, PosNames names) : IPosReturnReadStore
{
    public async Task<PosReturnResponse?> GetAsync(Guid returnId, CancellationToken ct)
    {
        var posReturn = await db.Set<PosReturn>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Refunds)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == returnId, ct);
        if (posReturn is null)
            return null;
        var terminal = await db.Set<PosTerminal>().AsNoTracking().Where(x => x.Id == posReturn.TerminalId)
            .Select(x => new { x.Code, x.Name, x.ReceiptHeader, x.ReceiptFooter }).SingleAsync(ct);
        var shiftNumber = await db.Set<PosShift>().AsNoTracking().Where(x => x.Id == posReturn.ShiftId).Select(x => x.Number).SingleAsync(ct);
        var sale = await db.Set<PosSale>().AsNoTracking().Where(x => x.Id == posReturn.SaleId)
            .Select(x => new { x.SoldAtUtc, x.CurrencyDecimals }).SingleAsync(ct);
        var lookup = await names.LoadAsync(branchIds: [posReturn.BranchId], warehouseIds: [posReturn.WarehouseId],
            userIds: [posReturn.CashierUserId], company: true, ct: ct);

        return new PosReturnResponse(
            posReturn.Id, posReturn.Number, IsCopy: true, CopyNumber: null, posReturn.PrintCount, lookup.CompanyName, lookup.TimeZoneId,
            posReturn.BranchId, lookup.BranchCode(posReturn.BranchId), lookup.BranchName(posReturn.BranchId), posReturn.WarehouseId,
            lookup.WarehouseCode(posReturn.WarehouseId), posReturn.TerminalId, terminal.Code, terminal.Name, terminal.ReceiptHeader,
            terminal.ReceiptFooter, posReturn.ShiftId, shiftNumber, posReturn.CashierUserId, lookup.UserName(posReturn.CashierUserId),
            posReturn.SaleId, posReturn.SaleNumber, sale.SoldAtUtc, posReturn.CustomerId, posReturn.CustomerName, posReturn.Reason,
            posReturn.Notes, posReturn.CurrencyCode, sale.CurrencyDecimals, posReturn.ReturnedAtUtc,
            posReturn.Lines.OrderBy(l => l.LineNumber).Select(l => new PosReturnLineResponse(
                l.Id, l.LineNumber, l.SaleLineId, l.ProductId, l.Sku, l.ProductName, l.ProductUnitId, l.UnitCode, l.Quantity, l.Condition,
                l.UnitPrice, l.DiscountPercent, l.TaxTreatment, l.TaxRate, l.NetAmount, l.TaxAmount, l.TotalAmount)).ToList(),
            posReturn.NetAmount, posReturn.TaxAmount, posReturn.TotalAmount,
            posReturn.Refunds.OrderBy(r => r.LineNumber)
                .Select(r => new PosRefundResponse(r.LineNumber, r.Method, r.Status, r.Amount, r.Reference, r.CardLast4)).ToList(),
            posReturn.CashRefundAmount, posReturn.CardRefundAmount, posReturn.StockDocumentId, posReturn.StockDocumentNumber);
    }

    public async Task<PagedResult<PosReturnSummaryResponse>> ListAsync(PosReturnFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<PosReturn>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.TerminalId is { } terminalId)
            query = query.Where(x => x.TerminalId == terminalId);
        if (filter.ShiftId is { } shiftId)
            query = query.Where(x => x.ShiftId == shiftId);
        if (filter.SaleId is { } saleId)
            query = query.Where(x => x.SaleId == saleId);
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.Search is { } search)
            query = query.Where(x => x.Number.Contains(search) || x.SaleNumber.Contains(search));
        if (filter.ReturnedFromUtc is { } from)
            query = query.Where(x => x.ReturnedAtUtc >= from);
        if (filter.ReturnedBeforeUtc is { } before)
            query = query.Where(x => x.ReturnedAtUtc < before);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.ReturnedAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Join(db.Set<PosTerminal>(), r => r.TerminalId, t => t.Id, (r, t) => new { Return = r, TerminalCode = t.Code })
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.Return.WarehouseId),
            userIds: rows.Select(r => (Guid?)r.Return.CashierUserId), ct: ct);
        return new PagedResult<PosReturnSummaryResponse>(
            rows.Select(r => new PosReturnSummaryResponse(
                r.Return.Id, r.Return.Number, r.Return.SaleId, r.Return.SaleNumber, r.Return.TerminalId, r.TerminalCode, r.Return.ShiftId,
                r.Return.WarehouseId, lookup.WarehouseCode(r.Return.WarehouseId), r.Return.CashierUserId,
                lookup.UserName(r.Return.CashierUserId), r.Return.CustomerName, r.Return.ReturnedAtUtc, r.Return.Reason,
                r.Return.TotalAmount, r.Return.CashRefundAmount, r.Return.CardRefundAmount)).ToList(),
            page, pageSize, total);
    }
}
