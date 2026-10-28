using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// An order to one supplier for goods into one physical warehouse (its branch numbers it), in the company's base
/// currency. Its lines change only while it is a draft. Approval (or release, when its policy needs none) numbers it and
/// fixes its prices; goods are then received in one or more receipts, never beyond what was ordered. It becomes Received
/// once every line is in, or Closed by hand with what is left; returns never reopen it. Each line keeps what was received,
/// invoiced and returned, which is what invoices and returns are checked against.
/// </summary>
public sealed class PurchaseOrder : IAuditable
{
    public const int MaxLines = 200;

    private readonly List<PurchaseOrderLine> _lines = [];
    private readonly List<PurchaseOrderEvent> _events = [];

    private PurchaseOrder() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DPO-000123), from an identity column; gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    public string? Number { get; private set; }
    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }

    /// <summary>The warehouse's branch, which scopes the number series.</summary>
    public Guid BranchId { get; private set; }

    /// <summary>The company's base currency when the order was created (V1 buys in the base currency only).</summary>
    public string CurrencyCode { get; private set; } = null!;

    public PurchaseOrderStatus Status { get; private set; }

    /// <summary>The approval policy when the order was created; later policy changes leave it alone.</summary>
    public bool RequiresApproval { get; private set; }

    public DateOnly? ExpectedDeliveryDate { get; private set; }

    /// <summary>The supplier's quotation or reference for this order.</summary>
    public string? SupplierReference { get; private set; }

    /// <summary>Days from invoice date to due date for this order's invoices.</summary>
    public int PaymentTermDays { get; private set; }

    public string? Notes { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? ClosedReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<PurchaseOrderLine> Lines => _lines;
    public IReadOnlyList<PurchaseOrderEvent> Events => _events;

    /// <summary>True once any goods were received; it can then only be closed, not cancelled.</summary>
    public bool HasReceipts => _lines.Any(l => l.ReceivedQuantity > 0);

    public static Result<PurchaseOrder> CreateDraft(
        Guid warehouseId, Guid branchId, string currencyCode, bool requiresApproval, OrderTerms terms,
        IReadOnlyList<OrderLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var order = new PurchaseOrder
        {
            WarehouseId = warehouseId,
            BranchId = branchId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = PurchaseOrderStatus.Draft,
            RequiresApproval = requiresApproval,
        };
        var filled = order.Fill(terms, lines);
        if (filled.IsFailure)
            return filled.Error;
        order.Record(PurchasingDocumentAction.Created, userId, nowUtc, note: null);
        return order;
    }

    /// <summary>Replaces a draft's supplier, terms and lines; the warehouse stays.</summary>
    public Result ReplaceDraft(OrderTerms terms, IReadOnlyList<OrderLineInput> lines) =>
        Status != PurchaseOrderStatus.Draft ? PurchaseOrderDomainErrors.NotDraft : Fill(terms, lines);

    /// <summary>Sends a draft that needs approval to the approvers.</summary>
    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != PurchaseOrderStatus.Draft)
            return PurchaseOrderDomainErrors.NotDraft;
        if (!RequiresApproval)
            return PurchaseOrderDomainErrors.ApprovalNotRequired;

        Status = PurchaseOrderStatus.Submitted;
        SubmittedByUserId = userId;
        Record(PurchasingDocumentAction.Submitted, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Releases a draft whose policy needs no approval: it is approved and numbered at once.</summary>
    public Result ApproveDirectly(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        if (Status != PurchaseOrderStatus.Draft)
            return PurchaseOrderDomainErrors.NotDraft;
        if (RequiresApproval)
            return PurchaseOrderDomainErrors.ApprovalRequired;

        SubmittedByUserId = userId;
        MarkApproved(number, userId, nowUtc);
        return Result.Success();
    }

    /// <summary>Only a submitted order is approved. Whoever approves may have created or submitted it (CLAUDE.md).</summary>
    public Result CanApprove() =>
        Status == PurchaseOrderStatus.Submitted ? Result.Success() : PurchaseOrderDomainErrors.NotSubmitted;

    public Result Approve(DocumentNumber number, Guid? approverId, DateTime nowUtc)
    {
        var allowed = CanApprove();
        if (allowed.IsFailure)
            return allowed;
        MarkApproved(number, approverId, nowUtc);
        return Result.Success();
    }

    /// <summary>Sends a submitted order back to draft, saying why.</summary>
    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != PurchaseOrderStatus.Submitted)
            return PurchaseOrderDomainErrors.NotSubmitted;

        Status = PurchaseOrderStatus.Draft;
        SubmittedByUserId = null;
        Record(PurchasingDocumentAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    /// <summary>Abandons the order before anything was received.</summary>
    public Result Cancel(string? reason, Guid? userId, DateTime nowUtc)
    {
        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Submitted or PurchaseOrderStatus.Approved))
            return PurchaseOrderDomainErrors.NotCancellable;
        if (HasReceipts)
            return PurchaseOrderDomainErrors.NotCancellable;

        Status = PurchaseOrderStatus.Cancelled;
        Record(PurchasingDocumentAction.Cancelled, userId, nowUtc, Text.Optional(reason));
        return Result.Success();
    }

    /// <summary>Closes what was not received after a partial receipt; it will never be received.</summary>
    public Result Close(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != PurchaseOrderStatus.PartiallyReceived)
            return Status == PurchaseOrderStatus.Approved ? PurchaseOrderDomainErrors.NothingReceivedToClose : PurchaseOrderDomainErrors.NotClosable;

        Status = PurchaseOrderStatus.Closed;
        ClosedReason = reason.Trim();
        Record(PurchasingDocumentAction.Closed, userId, nowUtc, ClosedReason);
        return Result.Success();
    }

    /// <summary>
    /// Records goods received against the order's lines: each at most what is still outstanding (no over-receipt), one
    /// entry per line. The order becomes Received when every line is complete.
    /// </summary>
    public Result Receive(IReadOnlyList<ReceiptLineInput> lines, string receiptNumber, Guid? userId, DateTime nowUtc)
    {
        if (Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
            return PurchaseOrderDomainErrors.NotReceivable;
        var checkedLines = CheckLines(lines.Select(l => l.OrderLineId).ToList());
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = checkedLines.Value[i];
            if (lines[i].Quantity > line.RemainingQuantity)
                return PurchaseOrderDomainErrors.OverReceipt(i + 1, line.RemainingQuantity);
        }

        for (var i = 0; i < lines.Count; i++)
            checkedLines.Value[i].AddReceived(lines[i].Quantity);
        Status = _lines.All(l => l.RemainingQuantity == 0) ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        Record(PurchasingDocumentAction.GoodsReceived, userId, nowUtc, receiptNumber);
        return Result.Success();
    }

    /// <summary>
    /// Records a posted invoice against the order's lines: each at most what was received and is not yet invoiced or
    /// returned before invoicing, so nothing is invoiced twice.
    /// </summary>
    public Result RecordInvoice(IReadOnlyList<InvoiceMatch> matches, string invoiceNumber, Guid? userId, DateTime nowUtc)
    {
        if (!IsCommitted)
            return PurchaseOrderDomainErrors.NotApproved;
        var checkedLines = CheckLines(matches.Select(m => m.OrderLineId).ToList());
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        for (var i = 0; i < matches.Count; i++)
        {
            var line = checkedLines.Value[i];
            if (matches[i].Quantity > line.InvoiceableQuantity)
                return PurchaseOrderDomainErrors.InvoiceExceedsReceipts(i + 1, line.InvoiceableQuantity);
        }

        for (var i = 0; i < matches.Count; i++)
            checkedLines.Value[i].AddInvoiced(matches[i].Quantity, matches[i].NetAmount, matches[i].TaxAmount);
        Record(PurchasingDocumentAction.Invoiced, userId, nowUtc, invoiceNumber);
        return Result.Success();
    }

    /// <summary>
    /// Records goods returned to the supplier. Each returned quantity is first taken from what was received but not yet
    /// invoiced (it will simply not be invoiced), then from what was invoiced (the supplier owes a credit, valued at the
    /// line's average invoiced net price and tax). Returns the split per line, in the order given.
    /// </summary>
    public Result<IReadOnlyList<ReturnSplit>> RecordReturn(
        IReadOnlyList<ReturnedQuantity> returned, string returnNumber, Guid? userId, DateTime nowUtc)
    {
        if (!IsCommitted)
            return PurchaseOrderDomainErrors.NotApproved;
        var checkedLines = CheckLines(returned.Select(r => r.OrderLineId).ToList());
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        var decimals = Amounts.CurrencyDecimals(CurrencyCode);
        var splits = new List<ReturnSplit>(returned.Count);
        for (var i = 0; i < returned.Count; i++)
        {
            var line = checkedLines.Value[i];
            if (returned[i].Quantity > line.ReceivedQuantity - line.ReturnedQuantity)
                return PurchaseOrderDomainErrors.ReturnExceedsReceipts(i + 1, line.ReceivedQuantity - line.ReturnedQuantity);
            splits.Add(line.AddReturned(returned[i].Quantity, decimals));
        }
        Record(PurchasingDocumentAction.Returned, userId, nowUtc, returnNumber);
        return splits;
    }

    /// <summary>Approved at some point and not cancelled: goods, invoices and returns may refer to it.</summary>
    public bool IsCommitted => Status is PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived
        or PurchaseOrderStatus.Received or PurchaseOrderStatus.Closed;

    public PurchaseOrderLine? FindLine(Guid lineId) => _lines.SingleOrDefault(l => l.Id == lineId);

    private Result<List<PurchaseOrderLine>> CheckLines(List<Guid> lineIds)
    {
        if (lineIds.Count == 0)
            return PurchaseOrderDomainErrors.NoLines;
        if (lineIds.Distinct().Count() != lineIds.Count)
            return PurchaseOrderDomainErrors.DuplicateLine;
        var found = new List<PurchaseOrderLine>(lineIds.Count);
        for (var i = 0; i < lineIds.Count; i++)
        {
            if (FindLine(lineIds[i]) is not { } line)
                return PurchaseOrderDomainErrors.LineNotFound(i + 1);
            found.Add(line);
        }
        return found;
    }

    private void MarkApproved(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        Status = PurchaseOrderStatus.Approved;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        ApprovedByUserId = userId;
        ApprovedAtUtc = nowUtc;
        Record(PurchasingDocumentAction.Approved, userId, nowUtc, note: null);
    }

    private Result Fill(OrderTerms terms, IReadOnlyList<OrderLineInput> lines)
    {
        ArgumentNullException.ThrowIfNull(terms);
        if (lines.Count == 0)
            return PurchaseOrderDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return PurchaseOrderDomainErrors.TooManyLines;
        if (terms.PaymentTermDays is < 0 or > 365)
            return PurchaseOrderDomainErrors.PaymentTermsInvalid;
        var requested = lines.Where(l => l.PurchaseRequestLineId.HasValue).Select(l => l.PurchaseRequestLineId).ToList();
        if (requested.Distinct().Count() != requested.Count)
            return PurchaseOrderDomainErrors.RequestLineRepeated;

        var decimals = Amounts.CurrencyDecimals(CurrencyCode);
        var built = new List<PurchaseOrderLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = PurchaseOrderLine.Create(i + 1, lines[i], decimals);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        SupplierId = terms.SupplierId;
        ExpectedDeliveryDate = terms.ExpectedDeliveryDate;
        SupplierReference = Text.Optional(terms.SupplierReference);
        PaymentTermDays = terms.PaymentTermDays;
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

    private void Record(PurchasingDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(PurchaseOrderEvent.Create(action, userId, nowUtc, note));
}
