using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.ReadModels;

// What Accounting may learn about the tills, read without locks. A shift's takings are summed from its own transactions,
// so they equal what their journals put on the till's clearing account to the cent.
internal sealed class PosAccountingSource(DbContext db) : IPosAccountingSource
{
    public async Task<IReadOnlyList<PosPostedDocument>> ListPostedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var sales = await db.Set<PosSale>().AsNoTracking()
            .Where(x => x.SoldAtUtc >= fromUtc && x.SoldAtUtc < toUtc)
            .Select(x => new PosPostedDocument(x.Id, PosDocumentType.Sale, x.Number, x.SoldAtUtc))
            .ToListAsync(ct);
        var returns = await db.Set<PosReturn>().AsNoTracking()
            .Where(x => x.ReturnedAtUtc >= fromUtc && x.ReturnedAtUtc < toUtc)
            .Select(x => new PosPostedDocument(x.Id, PosDocumentType.Return, x.Number, x.ReturnedAtUtc))
            .ToListAsync(ct);
        var shifts = await db.Set<PosShift>().AsNoTracking()
            .Where(x => x.Status == PosShiftStatus.Closed && x.ClosedAtUtc >= fromUtc && x.ClosedAtUtc < toUtc)
            .Select(x => new PosPostedDocument(x.Id, PosDocumentType.Shift, x.Number, x.ClosedAtUtc!.Value))
            .ToListAsync(ct);
        return [.. sales.Concat(returns).Concat(shifts).OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)];
    }

    public Task<PosSaleAccounting?> GetSaleAsync(Guid saleId, CancellationToken ct) =>
        db.Set<PosSale>().AsNoTracking()
            .Where(x => x.Id == saleId)
            .Select(x => new PosSaleAccounting(
                x.Id, x.Number, x.BranchId, x.ShiftId, x.SoldAtUtc, x.StockDocumentId, x.NetAmount, x.TaxAmount, x.CashReceivedAmount,
                x.CardAmount))
            .SingleOrDefaultAsync(ct);

    public Task<PosReturnAccounting?> GetReturnAsync(Guid returnId, CancellationToken ct) =>
        db.Set<PosReturn>().AsNoTracking()
            .Where(x => x.Id == returnId)
            .Select(x => new PosReturnAccounting(
                x.Id, x.Number, x.SaleNumber, x.BranchId, x.ShiftId, x.ReturnedAtUtc, x.StockDocumentId, x.NetAmount, x.TaxAmount,
                x.CashRefundAmount, x.CardRefundAmount))
            .SingleOrDefaultAsync(ct);

    public async Task<PosShiftAccounting?> GetClosedShiftAsync(Guid shiftId, CancellationToken ct)
    {
        var shift = await db.Set<PosShift>().AsNoTracking()
            .Where(x => x.Id == shiftId && x.Status == PosShiftStatus.Closed)
            .Select(x => new
            {
                x.Id, x.Number, x.BranchId, x.TerminalId, x.ClosedAtUtc, x.CountedCash, x.ExpectedCashAtClose, x.CashDifference,
                x.IsForceClosed,
            })
            .SingleOrDefaultAsync(ct);
        if (shift is null)
            return null;
        var cashIn = await db.Set<PosSale>().AsNoTracking().Where(x => x.ShiftId == shiftId)
            .SumAsync(x => (decimal?)x.CashReceivedAmount, ct) ?? 0m;
        var cashOut = await db.Set<PosReturn>().AsNoTracking().Where(x => x.ShiftId == shiftId)
            .SumAsync(x => (decimal?)x.CashRefundAmount, ct) ?? 0m;
        return new PosShiftAccounting(
            shift.Id, shift.Number, shift.BranchId, shift.TerminalId, shift.ClosedAtUtc!.Value, cashIn - cashOut,
            shift.CountedCash!.Value, shift.ExpectedCashAtClose!.Value, shift.CashDifference!.Value, shift.IsForceClosed);
    }

    public async Task<decimal> TillCashBeforeAsync(DateTime beforeUtc, CancellationToken ct)
    {
        var open = db.Set<PosShift>().AsNoTracking()
            .Where(x => x.OpenedAtUtc < beforeUtc && (x.ClosedAtUtc == null || x.ClosedAtUtc >= beforeUtc))
            .Select(x => x.Id);
        var cashIn = await db.Set<PosSale>().AsNoTracking()
            .Where(x => open.Contains(x.ShiftId) && x.SoldAtUtc < beforeUtc)
            .SumAsync(x => (decimal?)x.CashReceivedAmount, ct) ?? 0m;
        var cashOut = await db.Set<PosReturn>().AsNoTracking()
            .Where(x => open.Contains(x.ShiftId) && x.ReturnedAtUtc < beforeUtc)
            .SumAsync(x => (decimal?)x.CashRefundAmount, ct) ?? 0m;
        return cashIn - cashOut;
    }

    public async Task<IReadOnlyList<PosOpenCardIncident>> ListOpenCardIncidentsAsync(CancellationToken ct) =>
        await db.Set<PosCardIncident>().AsNoTracking()
            .Where(x => x.Status == PosCardIncidentStatus.ReversalRequired)
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new PosOpenCardIncident(x.Id, x.BranchId, x.TerminalId, x.ShiftId, x.CardAmount, x.References, x.OccurredAtUtc))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PosShiftCashDifference>> ListCashDifferencesAsync(DateTime fromUtc, CancellationToken ct) =>
        await db.Set<PosShift>().AsNoTracking()
            .Where(x => x.Status == PosShiftStatus.Closed && x.ClosedAtUtc >= fromUtc && x.CashDifference != 0m)
            .OrderBy(x => x.ClosedAtUtc)
            .Select(x => new PosShiftCashDifference(
                x.Id, x.Number, x.BranchId, x.TerminalId, x.ClosedAtUtc!.Value, x.CashDifference!.Value, x.IsForceClosed))
            .ToListAsync(ct);
}
