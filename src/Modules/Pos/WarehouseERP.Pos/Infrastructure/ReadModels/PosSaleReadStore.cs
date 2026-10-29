using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.ReadModels;

internal sealed class PosSaleReadStore(DbContext db, PosNames names) : IPosSaleReadStore
{
    public async Task<PosSaleResponse?> GetAsync(Guid saleId, CancellationToken ct)
    {
        var sale = await db.Set<PosSale>().AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == saleId, ct);
        if (sale is null)
            return null;
        var terminal = await db.Set<PosTerminal>().AsNoTracking().Where(x => x.Id == sale.TerminalId)
            .Select(x => new { x.Code, x.Name, x.ReceiptHeader, x.ReceiptFooter }).SingleAsync(ct);
        var shiftNumber = await db.Set<PosShift>().AsNoTracking().Where(x => x.Id == sale.ShiftId).Select(x => x.Number).SingleAsync(ct);
        var returns = await db.Set<PosReturn>().AsNoTracking().Where(x => x.SaleId == saleId).OrderBy(x => x.ReturnedAtUtc)
            .Select(x => new PosReturnReferenceResponse(x.Id, x.Number, x.ReturnedAtUtc, x.TotalAmount)).ToListAsync(ct);
        var lookup = await names.LoadAsync(branchIds: [sale.BranchId], warehouseIds: [sale.WarehouseId], userIds: [sale.CashierUserId],
            company: true, ct: ct);

        var lines = sale.Lines.OrderBy(l => l.LineNumber).ToList();
        return new PosSaleResponse(
            sale.Id, sale.Number, IsTaxInvoice: false, IsCopy: true, CopyNumber: null, sale.PrintCount, lookup.CompanyName, lookup.TimeZoneId,
            sale.BranchId, lookup.BranchCode(sale.BranchId), lookup.BranchName(sale.BranchId), sale.WarehouseId,
            lookup.WarehouseCode(sale.WarehouseId), sale.TerminalId, terminal.Code, terminal.Name, terminal.ReceiptHeader,
            terminal.ReceiptFooter, sale.ShiftId, shiftNumber, sale.CashierUserId, lookup.UserName(sale.CashierUserId), sale.CustomerId,
            sale.CustomerName, sale.CustomerPhone, sale.CustomerTaxNumber, sale.CurrencyCode, sale.CurrencyDecimals, sale.SoldAtUtc,
            lines.Select(l => new PosSaleLineResponse(
                l.Id, l.LineNumber, l.ProductId, l.Sku, l.ProductName, l.ProductUnitId, l.UnitCode, l.Quantity, l.UnitPrice,
                PosReferences.WithTax(l.UnitPrice, l.TaxRate, sale.CurrencyDecimals), l.DiscountPercent, l.GrossAmount,
                l.DiscountAmount, l.NetAmount, l.TaxTreatment, l.TaxRate, l.TaxAmount, l.TotalAmount, l.ReturnedQuantity,
                l.ReturnableQuantity)).ToList(),
            lines.GroupBy(l => (l.TaxTreatment, l.TaxRate)).OrderBy(g => g.Key.TaxTreatment).ThenByDescending(g => g.Key.TaxRate)
                .Select(g => new PosTaxLineResponse(g.Key.TaxTreatment, g.Key.TaxRate, g.Sum(l => l.NetAmount), g.Sum(l => l.TaxAmount)))
                .ToList(),
            sale.GrossAmount, sale.DiscountAmount, sale.NetAmount, sale.TaxAmount, sale.TotalAmount,
            sale.Payments.OrderBy(p => p.LineNumber).Select(p => new PosPaymentResponse(
                p.LineNumber, p.Method, p.Status, p.Amount, p.TenderedAmount, p.ChangeAmount, p.Reference, p.CardLast4)).ToList(),
            sale.CashTenderedAmount, sale.ChangeAmount, sale.CashReceivedAmount, sale.CardAmount, sale.RefundedAmount, returns,
            sale.StockDocumentId, sale.StockDocumentNumber, sale.Notes);
    }

    public async Task<PagedResult<PosSaleSummaryResponse>> ListAsync(PosSaleFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<PosSale>().AsNoTracking();
        if (filter.ScopeWarehouseIds is { } scope)
        {
            var ids = scope.ToArray();
            query = query.Where(x => ids.Contains(x.WarehouseId));
        }
        if (filter.TerminalId is { } terminalId)
            query = query.Where(x => x.TerminalId == terminalId);
        if (filter.ShiftId is { } shiftId)
            query = query.Where(x => x.ShiftId == shiftId);
        if (filter.WarehouseId is { } warehouseId)
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (filter.CustomerId is { } customerId)
            query = query.Where(x => x.CustomerId == customerId);
        if (filter.CashierUserId is { } cashierUserId)
            query = query.Where(x => x.CashierUserId == cashierUserId);
        if (filter.Search is { } search)
            query = query.Where(x => x.Number.Contains(search) || (x.CustomerName != null && x.CustomerName.Contains(search))
                                     || (x.CustomerPhone != null && x.CustomerPhone.Contains(search)));
        if (filter.SoldFromUtc is { } from)
            query = query.Where(x => x.SoldAtUtc >= from);
        if (filter.SoldBeforeUtc is { } before)
            query = query.Where(x => x.SoldAtUtc < before);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.SoldAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Join(db.Set<PosTerminal>(), s => s.TerminalId, t => t.Id, (s, t) => new
            {
                s.Id, s.Number, s.TerminalId, TerminalCode = t.Code, s.ShiftId, s.WarehouseId, s.CashierUserId, s.CustomerId,
                s.CustomerName, s.SoldAtUtc, LineCount = s.Lines.Count, s.TotalAmount, s.CashReceivedAmount, s.CardAmount,
                Refunded = s.RefundedCashAmount + s.RefundedCardAmount, s.ReturnCount,
            })
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(warehouseIds: rows.Select(r => r.WarehouseId), userIds: rows.Select(r => (Guid?)r.CashierUserId), ct: ct);
        return new PagedResult<PosSaleSummaryResponse>(
            rows.Select(r => new PosSaleSummaryResponse(
                r.Id, r.Number, r.TerminalId, r.TerminalCode, r.ShiftId, r.WarehouseId, lookup.WarehouseCode(r.WarehouseId),
                r.CashierUserId, lookup.UserName(r.CashierUserId), r.CustomerId, r.CustomerName, r.SoldAtUtc, r.LineCount,
                r.TotalAmount, r.CashReceivedAmount, r.CardAmount, r.Refunded, r.ReturnCount)).ToList(),
            page, pageSize, total);
    }
}
