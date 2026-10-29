using WarehouseERP.Pos.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// Goods a customer brought back against one completed POS sale, and the money refunded for them, recorded at a terminal
/// of the sale's warehouse on its cashier's open shift. Each line takes back at most what its sale line sold and has not
/// had back, at the price, discount and VAT it was sold with (cumulative shares, so returns add up to the sale). Only goods
/// fit for resale come back, into the terminal's selling location at the cost they left with (a PosReturn stock document);
/// damaged or uninspected goods are refused until Inventory has non-sellable stock. Refunds add up to exactly what the
/// lines are worth, each back the way the sale was paid. Recorded in one transaction with its stock document and number;
/// afterwards only reprints change it.
/// </summary>
public sealed class PosReturn : IAuditable
{
    public const int MaxReasonLength = 200;

    private readonly List<PosReturnLine> _lines = [];
    private readonly List<PosRefund> _refunds = [];

    private PosReturn() { } // EF Core

    public Guid Id { get; private set; }
    public string Number { get; private set; } = null!;
    public short FiscalYear { get; private set; }
    public int SequenceNumber { get; private set; }
    public Guid SaleId { get; private set; }
    public string SaleNumber { get; private set; } = null!;
    public Guid TerminalId { get; private set; }
    public Guid ShiftId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }

    /// <summary>The terminal's selling location the goods came back into.</summary>
    public Guid LocationId { get; private set; }

    public Guid CashierUserId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? CustomerName { get; private set; }
    public string Reason { get; private set; } = null!;
    public string? Notes { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }

    /// <summary>What was refunded: net plus VAT.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>What the goods were worth at cost coming back.</summary>
    public decimal CostAmount { get; private set; }

    public decimal CashRefundAmount { get; private set; }
    public decimal CardRefundAmount { get; private set; }
    public Guid? StockDocumentId { get; private set; }
    public string? StockDocumentNumber { get; private set; }
    public DateTime ReturnedAtUtc { get; private set; }
    public int PrintCount { get; private set; }
    public DateTime? LastPrintedAtUtc { get; private set; }
    public Guid? LastPrintedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<PosReturnLine> Lines => _lines;
    public IReadOnlyList<PosRefund> Refunds => _refunds;

    /// <summary>
    /// The return of these shares of the sale, refunded as entered, on an open shift of a terminal in the sale's warehouse.
    /// The sale and the shift record the refund; nothing is posted yet: the caller posts it to stock and marks it posted in
    /// the same transaction.
    /// </summary>
    public static Result<PosReturn> Create(
        DocumentNumber number, PosSale sale, PosShift shift, PosTerminal terminal, string? reason, string? notes,
        IReadOnlyList<PosReturnLineInput> lines, IReadOnlyList<PosPaymentInput> refunds, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(shift);
        ArgumentNullException.ThrowIfNull(terminal);
        if (terminal.WarehouseId != sale.WarehouseId)
            return PosReturnDomainErrors.OtherWarehouse;
        if (lines.Count == 0)
            return PosReturnDomainErrors.NoLines;
        if (lines.Count > PosSale.MaxLines)
            return PosReturnDomainErrors.TooManyLines;
        var why = Text.Optional(reason);
        if (why is null || why.Length > MaxReasonLength)
            return PosReturnDomainErrors.ReasonRequired;
        var text = Text.Optional(notes);
        if (text?.Length > PosSale.MaxNotesLength)
            return PosSaleDomainErrors.NotesTooLong;
        for (var i = 0; i < lines.Count; i++)
            if (lines[i].Condition != PosReturnCondition.FitForResale)
                return PosReturnDomainErrors.ConditionNotSupported(i + 1);

        var salesReturn = new PosReturn
        {
            Number = number.Number,
            FiscalYear = number.FiscalYear,
            SequenceNumber = number.SequenceNumber,
            SaleId = sale.Id,
            SaleNumber = sale.Number,
            TerminalId = terminal.Id,
            ShiftId = shift.Id,
            WarehouseId = terminal.WarehouseId,
            BranchId = terminal.BranchId,
            LocationId = terminal.SellingLocationId,
            CashierUserId = shift.CashierUserId,
            CustomerId = sale.CustomerId,
            CustomerName = sale.CustomerName,
            Reason = why,
            Notes = text,
            CurrencyCode = sale.CurrencyCode,
            ReturnedAtUtc = nowUtc,
            PrintCount = 1,
            LastPrintedAtUtc = nowUtc,
            LastPrintedByUserId = shift.CashierUserId,
        };
        for (var i = 0; i < lines.Count; i++)
            salesReturn._lines.Add(PosReturnLine.Create(i + 1, lines[i]));
        salesReturn.NetAmount = salesReturn._lines.Sum(l => l.NetAmount);
        salesReturn.TaxAmount = salesReturn._lines.Sum(l => l.TaxAmount);
        salesReturn.TotalAmount = salesReturn._lines.Sum(l => l.TotalAmount);

        var refunded = salesReturn.Refund(terminal, refunds, sale.CurrencyDecimals);
        if (refunded.IsFailure)
            return refunded.Error;
        var bySale = sale.RecordRefunds(salesReturn.CashRefundAmount, salesReturn.CardRefundAmount);
        if (bySale.IsFailure)
            return bySale.Error;
        var byShift = shift.RecordReturn(salesReturn.CashRefundAmount, salesReturn.CardRefundAmount, salesReturn.TotalAmount);
        return byShift.IsFailure ? byShift.Error : salesReturn;
    }

    /// <summary>Links the PosReturn stock document Inventory posted and each line's stock line and cost, in line order.</summary>
    public void MarkPosted(Guid stockDocumentId, string stockDocumentNumber, IReadOnlyList<(Guid StockLineId, decimal CostAmount)> lines)
    {
        if (StockDocumentId is not null)
            throw new InvalidOperationException($"Return {Id} is already posted to stock.");
        if (lines.Count != _lines.Count)
            throw new ArgumentException("Every return line has its stock line.", nameof(lines));

        var ordered = _lines.OrderBy(l => l.LineNumber).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].LinkStock(lines[i].StockLineId, lines[i].CostAmount);
        StockDocumentId = stockDocumentId;
        StockDocumentNumber = stockDocumentNumber;
        CostAmount = lines.Sum(l => l.CostAmount);
    }

    /// <summary>Counts a printed copy of the return receipt.</summary>
    public int RecordReprint(Guid? userId, DateTime nowUtc)
    {
        PrintCount++;
        LastPrintedAtUtc = nowUtc;
        LastPrintedByUserId = userId;
        return PrintCount;
    }

    // The refunds add up to exactly what the lines are worth; nothing is refunded for a return worth nothing.
    private Result Refund(PosTerminal terminal, IReadOnlyList<PosPaymentInput> refunds, int currencyDecimals)
    {
        if (refunds.Count > PosSale.MaxPayments)
            return PosSaleDomainErrors.TooManyPayments;
        if (refunds.Count(r => r.Method == PosPaymentMethod.Cash) > 1)
            return PosSaleDomainErrors.OneCashPaymentOnly;
        for (var i = 0; i < refunds.Count; i++)
        {
            var checkedRefund = PosSale.CheckPayment(terminal, refunds[i], currencyDecimals, i + 1);
            if (checkedRefund.IsFailure)
                return checkedRefund;
        }
        var refunded = refunds.Sum(r => r.Amount);
        if (refunded != TotalAmount)
            return PosReturnDomainErrors.RefundMismatch(TotalAmount, refunded);

        CashRefundAmount = refunds.Where(r => r.Method == PosPaymentMethod.Cash).Sum(r => r.Amount);
        CardRefundAmount = refunds.Where(r => r.Method == PosPaymentMethod.Card).Sum(r => r.Amount);
        for (var i = 0; i < refunds.Count; i++)
            _refunds.Add(PosRefund.Create(i + 1, refunds[i].Method, refunds[i].Amount, Text.Optional(refunds[i].Reference),
                Text.Optional(refunds[i].CardLast4)));
        return Result.Success();
    }
}
