using WarehouseERP.Pos.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// A completed till sale: its receipt number, the lines as Sales priced them (list price, discount, VAT, rounded amounts,
/// with names for the receipt), the payments, and the PosSale stock document Inventory posted with each line's cost. It is
/// recorded only together with that stock document, its payments and its receipt number, in one transaction; there is no
/// draft and no unpaid sale. Afterwards only returns (quantities and refunded amounts) and reprints change it.
/// </summary>
/// <remarks>
/// Payments: card payments never exceed the total; cash tendered covers the rest; change comes out of cash only. Cash
/// tendered, change given and cash received (tendered less change) are kept apart, and only cash received counts in the
/// drawer. Card payments are confirmed on a standalone card terminal and recorded with their reference; POS never sees a
/// card number beyond its last four digits.
/// </remarks>
public sealed class PosSale : IAuditable
{
    public const int MaxLines = 200;
    public const int MaxPayments = 10;
    public const int MaxCustomerNameLength = 200;
    public const int MaxPhoneLength = 30;
    public const int MaxNotesLength = 500;
    public const int MaxReferenceLength = 50;

    private readonly List<PosSaleLine> _lines = [];
    private readonly List<PosSalePayment> _payments = [];

    private PosSale() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>The receipt number, unique per terminal and year.</summary>
    public string Number { get; private set; } = null!;

    public short FiscalYear { get; private set; }
    public int SequenceNumber { get; private set; }
    public Guid TerminalId { get; private set; }
    public Guid ShiftId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }

    /// <summary>The terminal's selling location the goods left.</summary>
    public Guid LocationId { get; private set; }

    public Guid CashierUserId { get; private set; }

    /// <summary>A registered customer; null for a walk-in.</summary>
    public Guid? CustomerId { get; private set; }

    public string? CustomerName { get; private set; }
    public string? CustomerPhone { get; private set; }
    public string? CustomerTaxNumber { get; private set; }
    public string CurrencyCode { get; private set; } = null!;

    /// <summary>Decimals of the currency's minor unit the amounts are rounded to.</summary>
    public byte CurrencyDecimals { get; private set; }

    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }

    /// <summary>What the customer paid: net plus VAT.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>What the goods cost at the moving average.</summary>
    public decimal CostAmount { get; private set; }

    /// <summary>Cash the customer handed over.</summary>
    public decimal CashTenderedAmount { get; private set; }

    /// <summary>Change given back, out of the cash tendered.</summary>
    public decimal ChangeAmount { get; private set; }

    /// <summary>Cash that stayed in the drawer: tendered less change.</summary>
    public decimal CashReceivedAmount { get; private set; }

    public decimal CardAmount { get; private set; }
    public decimal RefundedCashAmount { get; private set; }
    public decimal RefundedCardAmount { get; private set; }
    public decimal RefundedAmount => RefundedCashAmount + RefundedCardAmount;
    public int ReturnCount { get; private set; }
    public Guid? StockDocumentId { get; private set; }
    public string? StockDocumentNumber { get; private set; }
    public DateTime SoldAtUtc { get; private set; }

    /// <summary>Receipts printed: 1 for the original at checkout, then one more per copy.</summary>
    public int PrintCount { get; private set; }

    public DateTime? LastPrintedAtUtc { get; private set; }
    public Guid? LastPrintedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<PosSaleLine> Lines => _lines;
    public IReadOnlyList<PosSalePayment> Payments => _payments;

    /// <summary>
    /// The sale of these priced lines on an open shift, paid as entered. Nothing is posted yet: the caller posts it to
    /// stock and marks it posted in the same transaction.
    /// </summary>
    public static Result<PosSale> Create(
        DocumentNumber number, PosShift shift, PosTerminal terminal, PosSaleCustomer customer, string currencyCode, int currencyDecimals,
        IReadOnlyList<PosSaleLineInput> lines, IReadOnlyList<PosPaymentInput> payments, string? notes, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(shift);
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(customer);
        if (!shift.IsOpen)
            return PosShiftDomainErrors.NotOpen;
        if (lines.Count == 0)
            return PosSaleDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return PosSaleDomainErrors.TooManyLines;
        var name = Text.Optional(customer.Name);
        var phone = Text.Optional(customer.Phone);
        if (name?.Length > MaxCustomerNameLength || phone?.Length > MaxPhoneLength)
            return PosSaleDomainErrors.CustomerTooLong;
        var text = Text.Optional(notes);
        if (text?.Length > MaxNotesLength)
            return PosSaleDomainErrors.NotesTooLong;

        var sale = new PosSale
        {
            Number = number.Number,
            FiscalYear = number.FiscalYear,
            SequenceNumber = number.SequenceNumber,
            TerminalId = terminal.Id,
            ShiftId = shift.Id,
            WarehouseId = terminal.WarehouseId,
            BranchId = terminal.BranchId,
            LocationId = terminal.SellingLocationId,
            CashierUserId = shift.CashierUserId,
            CustomerId = customer.CustomerId,
            CustomerName = name,
            CustomerPhone = phone,
            CustomerTaxNumber = Text.Optional(customer.TaxNumber),
            CurrencyCode = currencyCode,
            CurrencyDecimals = checked((byte)currencyDecimals),
            SoldAtUtc = nowUtc,
            PrintCount = 1,
            LastPrintedAtUtc = nowUtc,
            LastPrintedByUserId = shift.CashierUserId,
            Notes = text,
        };
        for (var i = 0; i < lines.Count; i++)
            sale._lines.Add(PosSaleLine.Create(i + 1, lines[i]));
        sale.GrossAmount = sale._lines.Sum(l => l.GrossAmount);
        sale.DiscountAmount = sale._lines.Sum(l => l.DiscountAmount);
        sale.NetAmount = sale._lines.Sum(l => l.NetAmount);
        sale.TaxAmount = sale._lines.Sum(l => l.TaxAmount);
        sale.TotalAmount = sale._lines.Sum(l => l.TotalAmount);
        if (sale.TotalAmount > Money.MaxAmount)
            return PosSaleDomainErrors.TotalTooLarge;

        var paid = sale.Pay(terminal, payments, currencyDecimals);
        if (paid.IsFailure)
            return paid.Error;
        var recorded = shift.RecordSale(sale.CashReceivedAmount, sale.CardAmount, sale.TotalAmount);
        return recorded.IsFailure ? recorded.Error : sale;
    }

    /// <summary>Links the PosSale stock document Inventory posted and each line's stock line and cost, in line order.</summary>
    public void MarkPosted(Guid stockDocumentId, string stockDocumentNumber, IReadOnlyList<(Guid StockLineId, decimal CostAmount)> lines)
    {
        if (StockDocumentId is not null)
            throw new InvalidOperationException($"Sale {Id} is already posted to stock.");
        if (lines.Count != _lines.Count)
            throw new ArgumentException("Every sale line has its stock line.", nameof(lines));

        var ordered = _lines.OrderBy(l => l.LineNumber).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].LinkStock(lines[i].StockLineId, lines[i].CostAmount);
        StockDocumentId = stockDocumentId;
        StockDocumentNumber = stockDocumentNumber;
        CostAmount = lines.Sum(l => l.CostAmount);
    }

    /// <summary>
    /// Takes these quantities back from its lines and works out what each returns at the price it was sold for: every line
    /// keeps cumulative refunded net and VAT, so partial returns are cumulative shares of the line and the last units back
    /// take exactly what is left. Never more than a line sold and has not had back.
    /// </summary>
    public Result<IReadOnlyList<ReturnShare>> TakeBack(IReadOnlyList<(Guid SaleLineId, decimal Quantity)> returned)
    {
        if (StockDocumentId is null)
            return PosSaleDomainErrors.NotPosted;
        for (var i = 0; i < returned.Count; i++)
        {
            var line = FindLine(returned[i].SaleLineId);
            if (line is null)
                return PosSaleDomainErrors.LineNotFound(i + 1);
            if (returned[i].Quantity <= 0 || !Money.FitsScale(returned[i].Quantity, Money.QuantityScale))
                return PosSaleDomainErrors.ReturnQuantityInvalid(i + 1);
        }
        foreach (var group in returned.Select((r, i) => (Line: r, Number: i + 1)).GroupBy(x => x.Line.SaleLineId))
        {
            var line = FindLine(group.Key)!;
            if (group.Sum(x => x.Line.Quantity) > line.ReturnableQuantity)
                return PosSaleDomainErrors.ReturnExceedsSale(group.First().Number, line.ReturnableQuantity);
        }

        var shares = new List<ReturnShare>(returned.Count);
        foreach (var (lineId, quantity) in returned)
            shares.Add(FindLine(lineId)!.TakeBack(quantity, CurrencyDecimals));
        return shares;
    }

    /// <summary>
    /// Records refunds of a return, each back the way it was paid: cash refunds up to the cash received less cash already
    /// refunded, card refunds up to the card payments less card refunds; so refunds never exceed what was actually paid.
    /// </summary>
    public Result RecordRefunds(decimal cashRefund, decimal cardRefund)
    {
        if (cashRefund > CashReceivedAmount - RefundedCashAmount)
            return PosSaleDomainErrors.CashRefundExceedsPaid(CashReceivedAmount - RefundedCashAmount);
        if (cardRefund > CardAmount - RefundedCardAmount)
            return PosSaleDomainErrors.CardRefundExceedsPaid(CardAmount - RefundedCardAmount);
        RefundedCashAmount += cashRefund;
        RefundedCardAmount += cardRefund;
        ReturnCount++;
        return Result.Success();
    }

    /// <summary>Counts a printed copy of the receipt.</summary>
    public int RecordReprint(Guid? userId, DateTime nowUtc)
    {
        PrintCount++;
        LastPrintedAtUtc = nowUtc;
        LastPrintedByUserId = userId;
        return PrintCount;
    }

    public PosSaleLine? FindLine(Guid lineId) => _lines.SingleOrDefault(l => l.Id == lineId);

    // Card payments never exceed the total; cash tendered pays the rest, and change comes out of it.
    private Result Pay(PosTerminal terminal, IReadOnlyList<PosPaymentInput> payments, int currencyDecimals)
    {
        if (payments.Count == 0)
            return PosSaleDomainErrors.NoPayment;
        if (payments.Count > MaxPayments)
            return PosSaleDomainErrors.TooManyPayments;
        if (payments.Count(p => p.Method == PosPaymentMethod.Cash) > 1)
            return PosSaleDomainErrors.OneCashPaymentOnly;
        for (var i = 0; i < payments.Count; i++)
        {
            var checkedPayment = CheckPayment(terminal, payments[i], currencyDecimals, i + 1);
            if (checkedPayment.IsFailure)
                return checkedPayment;
        }

        var card = payments.Where(p => p.Method == PosPaymentMethod.Card).Sum(p => p.Amount);
        var tendered = payments.Where(p => p.Method == PosPaymentMethod.Cash).Sum(p => p.Amount);
        if (card > TotalAmount)
            return PosSaleDomainErrors.CardExceedsTotal;
        if (card + tendered < TotalAmount)
            return PosSaleDomainErrors.Underpaid(TotalAmount - card - tendered);
        var change = card + tendered - TotalAmount;
        var received = tendered - change;
        if (tendered > 0 && received == 0)
            return PosSaleDomainErrors.CashNotNeeded;

        CardAmount = card;
        CashTenderedAmount = tendered;
        ChangeAmount = change;
        CashReceivedAmount = received;
        for (var i = 0; i < payments.Count; i++)
        {
            var payment = payments[i];
            _payments.Add(payment.Method == PosPaymentMethod.Cash
                ? PosSalePayment.Cash(i + 1, received, tendered, change)
                : PosSalePayment.Card(i + 1, payment.Amount, payment.Reference!.Trim(), Text.Optional(payment.CardLast4)));
        }
        return Result.Success();
    }

    /// <summary>One payment or refund line as entered: a method the terminal accepts, an amount, a card's reference.</summary>
    internal static Result CheckPayment(PosTerminal terminal, PosPaymentInput payment, int currencyDecimals, int number)
    {
        if (!Enum.IsDefined(payment.Method))
            return PosSaleDomainErrors.PaymentMethodInvalid(number);
        if (!terminal.Accepts(payment.Method))
            return PosSaleDomainErrors.PaymentMethodNotAccepted(number);
        if (!Money.IsAmount(payment.Amount, currencyDecimals))
            return PosSaleDomainErrors.PaymentAmountInvalid(number);
        if (payment.Method == PosPaymentMethod.Card)
        {
            var reference = Text.Optional(payment.Reference);
            if (reference is null || reference.Length > MaxReferenceLength)
                return PosSaleDomainErrors.CardReferenceRequired(number);
            if (Text.Optional(payment.CardLast4) is { } last4 && (last4.Length != 4 || !last4.All(char.IsAsciiDigit)))
                return PosSaleDomainErrors.CardLast4Invalid(number);
        }
        else if (payment.Reference is not null && payment.Reference.Length > MaxReferenceLength)
            return PosSaleDomainErrors.CardReferenceRequired(number);
        return Result.Success();
    }
}
