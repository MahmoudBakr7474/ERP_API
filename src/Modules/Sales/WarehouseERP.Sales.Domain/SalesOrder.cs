using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// An order from one customer for goods from one physical warehouse that allows selling (its branch numbers it), in the
/// company's base currency, its prices, discounts and taxes a snapshot taken when the lines were entered. Lines change only
/// while it is a draft. Submitting confirms it at once, or sends it to an approver when the sales settings ask;
/// confirmation numbers it and, in the same transaction, Inventory reserves every line or nothing. Goods then leave in
/// one or more deliveries, never beyond what remains; a line can be reduced and the whole order closed, which releases the
/// reservation of what will never be delivered; it is cancelled only before any delivery. Invoices bill what was delivered,
/// returns bring delivered goods back; neither reopens it.
/// </summary>
public sealed class SalesOrder : IAuditable
{
    public const int MaxLines = 200;

    private readonly List<SalesOrderLine> _lines = [];
    private readonly List<SalesOrderEvent> _events = [];

    private SalesOrder() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DSO-000123), from an identity column; gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    public string? Number { get; private set; }
    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }

    /// <summary>The warehouse's branch, which scopes the number series.</summary>
    public Guid BranchId { get; private set; }

    public string CurrencyCode { get; private set; } = null!;
    public SalesOrderStatus Status { get; private set; }
    public DateOnly OrderDate { get; private set; }
    public DateOnly? RequestedDeliveryDate { get; private set; }
    public string? CustomerReference { get; private set; }

    /// <summary>Days from invoice date to due date for this order's invoices.</summary>
    public int PaymentTermDays { get; private set; }

    public string? ShippingAddress { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>The quotation the order came from.</summary>
    public Guid? SourceQuotationId { get; private set; }

    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    /// <summary>Total of the posted invoices.</summary>
    public decimal InvoicedAmount { get; private set; }

    /// <summary>Credits owed to the customer for invoiced goods that came back (net plus tax), pending Accounting.</summary>
    public decimal CreditedAmount { get; private set; }

    /// <summary>Why it went to an approver (<see cref="Domain.ApprovalReasons"/> flags); 0 when confirmed directly.</summary>
    public int ApprovalReasonFlags { get; private set; }

    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public string? ClosedReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<SalesOrderLine> Lines => _lines;
    public IReadOnlyList<SalesOrderEvent> Events => _events;

    /// <summary>True once anything was delivered; it can then only be closed, not cancelled.</summary>
    public bool HasDeliveries => _lines.Any(l => l.DeliveredQuantity > 0);

    /// <summary>Confirmed at some point and not cancelled: deliveries, invoices and returns may refer to it.</summary>
    public bool IsCommitted => Status is SalesOrderStatus.Confirmed or SalesOrderStatus.PartiallyDelivered
        or SalesOrderStatus.Delivered or SalesOrderStatus.Closed;

    /// <summary>Confirmed and something is still to be delivered (and reserved).</summary>
    public bool IsOpen => Status is SalesOrderStatus.Confirmed or SalesOrderStatus.PartiallyDelivered;

    public static Result<SalesOrder> CreateDraft(
        Guid warehouseId, Guid branchId, string currencyCode, OrderTerms terms, IReadOnlyList<SalesLineInput> lines,
        Guid? sourceQuotationId, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var order = new SalesOrder
        {
            WarehouseId = warehouseId,
            BranchId = branchId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = SalesOrderStatus.Draft,
            SourceQuotationId = sourceQuotationId,
        };
        var filled = order.Fill(terms, lines);
        if (filled.IsFailure)
            return filled.Error;
        order.Record(SalesDocumentAction.Created, userId, nowUtc, note: null);
        return order;
    }

    /// <summary>Replaces a draft's customer, terms and lines; the warehouse stays.</summary>
    public Result ReplaceDraft(OrderTerms terms, IReadOnlyList<SalesLineInput> lines) =>
        Status != SalesOrderStatus.Draft ? SalesOrderDomainErrors.NotDraft : Fill(terms, lines);

    /// <summary>Sends a draft to an approver, saying why it needs one. Nothing is reserved yet.</summary>
    public Result Submit(IReadOnlyList<ApprovalReason> reasons, Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesOrderStatus.Draft)
            return SalesOrderDomainErrors.NotDraft;
        if (reasons.Count == 0)
            throw new ArgumentException("An order goes to an approver only for a reason.", nameof(reasons));

        Status = SalesOrderStatus.Submitted;
        SubmittedByUserId = userId;
        ApprovalReasonFlags = ApprovalReasons.ToFlags(reasons);
        Record(SalesDocumentAction.Submitted, userId, nowUtc, ApprovalReasons.Note(reasons));
        return Result.Success();
    }

    /// <summary>Confirms a draft that needs no approval; the caller reserves its stock in the same transaction.</summary>
    public Result ConfirmDirectly(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesOrderStatus.Draft)
            return SalesOrderDomainErrors.NotDraft;

        SubmittedByUserId = userId;
        ApprovalReasonFlags = 0;
        MarkConfirmed(number, userId, nowUtc);
        return Result.Success();
    }

    /// <summary>Only a submitted order is approved. Whoever approves may have created or submitted it (CLAUDE.md).</summary>
    public Result CanApprove() =>
        Status == SalesOrderStatus.Submitted ? Result.Success() : SalesOrderDomainErrors.NotSubmitted;

    /// <summary>Approving confirms it; the caller reserves its stock in the same transaction.</summary>
    public Result Approve(DocumentNumber number, Guid? approverId, DateTime nowUtc)
    {
        var allowed = CanApprove();
        if (allowed.IsFailure)
            return allowed;
        ApprovedByUserId = approverId;
        ApprovedAtUtc = nowUtc;
        Record(SalesDocumentAction.Approved, approverId, nowUtc, note: null);
        MarkConfirmed(number, approverId, nowUtc);
        return Result.Success();
    }

    /// <summary>Sends a submitted order back to draft, saying why.</summary>
    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != SalesOrderStatus.Submitted)
            return SalesOrderDomainErrors.NotSubmitted;

        Status = SalesOrderStatus.Draft;
        SubmittedByUserId = null;
        ApprovalReasonFlags = 0;
        Record(SalesDocumentAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    /// <summary>
    /// Abandons the order before anything was delivered. Returns the lines whose reservation the caller releases: all of
    /// them for a confirmed order, none before confirmation.
    /// </summary>
    public Result<IReadOnlyList<SalesOrderLine>> Cancel(string? reason, Guid? userId, DateTime nowUtc)
    {
        if (Status is not (SalesOrderStatus.Draft or SalesOrderStatus.Submitted or SalesOrderStatus.Confirmed) || HasDeliveries)
            return SalesOrderDomainErrors.NotCancellable;

        IReadOnlyList<SalesOrderLine> reserved = Status == SalesOrderStatus.Confirmed ? _lines.ToList() : [];
        Status = SalesOrderStatus.Cancelled;
        Record(SalesDocumentAction.Cancelled, userId, nowUtc, Text.Optional(reason));
        return Result.Success(reserved);
    }

    /// <summary>
    /// Takes part of an open line off the order: never more than remains to deliver, and never everything the order still
    /// had when nothing was delivered (cancel it instead). The caller releases the line's reservation for it.
    /// </summary>
    public Result<SalesOrderLine> ReduceLine(Guid lineId, decimal quantity, string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (!IsOpen)
            return SalesOrderDomainErrors.NotOpen;
        if (FindLine(lineId) is not { } line)
            return SalesOrderDomainErrors.LineNotFound(1);
        if (quantity <= 0 || !Amounts.FitsScale(quantity, Amounts.QuantityScale) || quantity > line.RemainingQuantity)
            return SalesOrderDomainErrors.ReductionInvalid(line.RemainingQuantity);
        if (!HasDeliveries && _lines.Sum(l => l.RemainingQuantity) == quantity)
            return SalesOrderDomainErrors.CancelInstead;

        line.AddClosed(quantity);
        UpdateFulfilment();
        Record(SalesDocumentAction.Reduced, userId, nowUtc, $"{line.LineNumber}: {reason.Trim()}");
        return line;
    }

    /// <summary>
    /// Closes what was not delivered after a partial delivery; it will never be delivered. Returns each line with what it
    /// still had, whose reservation the caller releases.
    /// </summary>
    public Result<IReadOnlyList<OrderLineQuantity>> Close(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != SalesOrderStatus.PartiallyDelivered)
            return Status == SalesOrderStatus.Confirmed ? SalesOrderDomainErrors.NothingDeliveredToClose : SalesOrderDomainErrors.NotClosable;

        var closed = _lines.Where(l => l.RemainingQuantity > 0).Select(l => new OrderLineQuantity(l.Id, l.RemainingQuantity)).ToList();
        foreach (var line in closed)
            FindLine(line.OrderLineId)!.AddClosed(line.Quantity);
        Status = SalesOrderStatus.Closed;
        ClosedReason = reason.Trim();
        Record(SalesDocumentAction.Closed, userId, nowUtc, ClosedReason);
        return closed;
    }

    /// <summary>Checks a delivery against what each line still has to deliver; nothing changes.</summary>
    public Result CanDeliver(IReadOnlyList<OrderLineQuantity> lines)
    {
        if (!IsOpen)
            return SalesOrderDomainErrors.NotOpen;
        var checkedLines = CheckLines(lines.Select(l => l.OrderLineId).ToList(), allowRepeats: true);
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        foreach (var group in lines.Select((l, i) => (Line: l, Number: i + 1)).GroupBy(x => x.Line.OrderLineId))
        {
            var line = FindLine(group.Key)!;
            if (group.Sum(x => x.Line.Quantity) > line.RemainingQuantity)
                return SalesOrderDomainErrors.OverDelivery(group.First().Number, line.RemainingQuantity);
        }
        return Result.Success();
    }

    /// <summary>
    /// Records a posted delivery against the order's lines, each at most what is still to deliver (several delivery lines
    /// may pick one order line from different locations). The order becomes delivered when nothing remains.
    /// </summary>
    public Result RecordDelivery(IReadOnlyList<OrderLineQuantity> lines, string deliveryNumber, Guid? userId, DateTime nowUtc)
    {
        var allowed = CanDeliver(lines);
        if (allowed.IsFailure)
            return allowed;
        foreach (var line in lines)
            FindLine(line.OrderLineId)!.AddDelivered(line.Quantity);
        UpdateFulfilment();
        Record(SalesDocumentAction.Delivered, userId, nowUtc, deliveryNumber);
        return Result.Success();
    }

    /// <summary>
    /// Records a posted invoice against the order's lines: each at most what was delivered and is not invoiced or returned
    /// before invoicing yet, so nothing is invoiced twice.
    /// </summary>
    public Result RecordInvoice(IReadOnlyList<InvoiceMatch> matches, decimal invoiceTotal, string invoiceNumber, Guid? userId, DateTime nowUtc)
    {
        var allowed = CanInvoice(matches.Select(m => new OrderLineQuantity(m.OrderLineId, m.Quantity)).ToList());
        if (allowed.IsFailure)
            return allowed;
        foreach (var match in matches)
            FindLine(match.OrderLineId)!.AddInvoiced(match.Quantity, match.NetAmount, match.TaxAmount);
        InvoicedAmount += invoiceTotal;
        Record(SalesDocumentAction.Invoiced, userId, nowUtc, invoiceNumber);
        return Result.Success();
    }

    /// <summary>Checks invoice quantities against what each line may still bill; nothing changes.</summary>
    public Result CanInvoice(IReadOnlyList<OrderLineQuantity> lines)
    {
        if (!IsCommitted)
            return SalesOrderDomainErrors.NotConfirmed;
        var checkedLines = CheckLines(lines.Select(l => l.OrderLineId).ToList(), allowRepeats: false);
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = checkedLines.Value[i];
            if (lines[i].Quantity > line.InvoiceableQuantity)
                return SalesOrderDomainErrors.InvoiceExceedsDeliveries(i + 1, line.InvoiceableQuantity);
        }
        return Result.Success();
    }

    /// <summary>
    /// Records goods the customer sent back. Each returned quantity is first taken from what was delivered but not
    /// invoiced (it simply won't be invoiced), then from what was invoiced (the company owes a credit, valued at the line's
    /// average invoiced net price and tax). Returns the split per line, in the order given.
    /// </summary>
    public Result<IReadOnlyList<ReturnSplit>> RecordReturn(
        IReadOnlyList<OrderLineQuantity> returned, string returnNumber, Guid? userId, DateTime nowUtc)
    {
        if (!IsCommitted)
            return SalesOrderDomainErrors.NotConfirmed;
        var checkedLines = CheckLines(returned.Select(r => r.OrderLineId).ToList(), allowRepeats: true);
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        foreach (var group in returned.Select((r, i) => (Line: r, Number: i + 1)).GroupBy(x => x.Line.OrderLineId))
        {
            var line = FindLine(group.Key)!;
            if (group.Sum(x => x.Line.Quantity) > line.ReturnableQuantity)
                return SalesOrderDomainErrors.ReturnExceedsDeliveries(group.First().Number, line.ReturnableQuantity);
        }

        var decimals = Amounts.CurrencyDecimals(CurrencyCode);
        var splits = returned.Select(r => FindLine(r.OrderLineId)!.AddReturned(r.Quantity, decimals)).ToList();
        CreditedAmount += splits.Sum(s => s.CreditNetAmount + s.CreditTaxAmount);
        Record(SalesDocumentAction.Returned, userId, nowUtc, returnNumber);
        return splits;
    }

    public SalesOrderLine? FindLine(Guid lineId) => _lines.SingleOrDefault(l => l.Id == lineId);

    // Confirmed until something is delivered; delivered (or closed, when something was taken off) once nothing remains.
    private void UpdateFulfilment()
    {
        if (_lines.All(l => l.RemainingQuantity == 0))
            Status = _lines.Any(l => l.ClosedQuantity > 0) ? SalesOrderStatus.Closed : SalesOrderStatus.Delivered;
        else if (HasDeliveries)
            Status = SalesOrderStatus.PartiallyDelivered;
    }

    // The order's lines in the order given; a document line number (from 1) names the first that is wrong.
    private Result<List<SalesOrderLine>> CheckLines(List<Guid> lineIds, bool allowRepeats)
    {
        if (lineIds.Count == 0)
            return SalesOrderDomainErrors.NoLines;
        if (!allowRepeats && lineIds.Distinct().Count() != lineIds.Count)
            return SalesOrderDomainErrors.DuplicateLine;
        var found = new List<SalesOrderLine>(lineIds.Count);
        for (var i = 0; i < lineIds.Count; i++)
        {
            if (FindLine(lineIds[i]) is not { } line)
                return SalesOrderDomainErrors.LineNotFound(i + 1);
            found.Add(line);
        }
        return found;
    }

    private void MarkConfirmed(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        Status = SalesOrderStatus.Confirmed;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        ConfirmedAtUtc = nowUtc;
        Record(SalesDocumentAction.Confirmed, userId, nowUtc, note: null);
    }

    private Result Fill(OrderTerms terms, IReadOnlyList<SalesLineInput> lines)
    {
        ArgumentNullException.ThrowIfNull(terms);
        if (lines.Count == 0)
            return SalesOrderDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return SalesOrderDomainErrors.TooManyLines;
        if (terms.PaymentTermDays is < 0 or > 365)
            return SalesOrderDomainErrors.PaymentTermsInvalid;
        if (terms.RequestedDeliveryDate is { } requested && requested < terms.OrderDate)
            return SalesOrderDomainErrors.RequestedDateInvalid;

        var decimals = Amounts.CurrencyDecimals(CurrencyCode);
        var built = new List<SalesOrderLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = SalesOrderLine.Create(i + 1, lines[i], decimals);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        CustomerId = terms.CustomerId;
        OrderDate = terms.OrderDate;
        RequestedDeliveryDate = terms.RequestedDeliveryDate;
        CustomerReference = Text.Optional(terms.CustomerReference);
        PaymentTermDays = terms.PaymentTermDays;
        ShippingAddress = Text.Optional(terms.ShippingAddress);
        Notes = Text.Optional(terms.Notes);
        _lines.Clear();
        _lines.AddRange(built);
        GrossAmount = built.Sum(l => l.GrossAmount);
        DiscountAmount = built.Sum(l => l.DiscountAmount);
        NetAmount = built.Sum(l => l.NetAmount);
        TaxAmount = built.Sum(l => l.TaxAmount);
        TotalAmount = built.Sum(l => l.TotalAmount);
        return Result.Success();
    }

    private void Record(SalesDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(SalesOrderEvent.Create(action, userId, nowUtc, note));
}
