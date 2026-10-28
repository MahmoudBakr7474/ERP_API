using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// A supplier's invoice for one approved order, matched line by line to the order: each line bills received goods of
/// one order line at the supplier's price. A line whose net unit price differs from the order's makes the invoice need
/// approval, and the difference is kept as a purchase price variance; posted inventory is never revalued
/// (V1's provisional treatment, pending the accountant). Posting records the liability (totals, tax and due date) for
/// future Accounts Payable, numbers the invoice and makes it immutable. The supplier's invoice number is unique per
/// supplier once normalized, for every invoice that is not cancelled.
/// </summary>
public sealed class SupplierInvoice : IAuditable
{
    public const int MaxLines = 200;

    private readonly List<SupplierInvoiceLine> _lines = [];
    private readonly List<SupplierInvoiceEvent> _events = [];

    private SupplierInvoice() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DSIN-000123), from an identity column.</summary>
    public int DraftNumber { get; private set; }

    /// <summary>The company's own number (SIN-...), given when it posts.</summary>
    public string? Number { get; private set; }

    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public string CurrencyCode { get; private set; } = null!;

    /// <summary>The supplier's invoice number as printed.</summary>
    public string SupplierInvoiceNumber { get; private set; } = null!;

    /// <summary>The supplier's invoice number normalized (SupplierInvoiceNumbers.Key): unique per supplier.</summary>
    public string SupplierInvoiceNumberKey { get; private set; } = null!;

    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string? Notes { get; private set; }
    public SupplierInvoiceStatus Status { get; private set; }

    /// <summary>True while a line's net unit price differs from the order's: an approver must approve it.</summary>
    public bool RequiresApproval { get; private set; }

    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    /// <summary>Sum of the lines' purchase price variances (invoice net price minus order net price, times quantity).</summary>
    public decimal PriceVarianceAmount { get; private set; }

    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<SupplierInvoiceLine> Lines => _lines;
    public IReadOnlyList<SupplierInvoiceEvent> Events => _events;

    public static Result<SupplierInvoice> CreateDraft(
        PurchaseOrder order, InvoiceTerms terms, IReadOnlyList<InvoiceLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!order.IsCommitted)
            return SupplierInvoiceDomainErrors.OrderNotApproved;

        var invoice = new SupplierInvoice
        {
            SupplierId = order.SupplierId,
            PurchaseOrderId = order.Id,
            WarehouseId = order.WarehouseId,
            BranchId = order.BranchId,
            CurrencyCode = order.CurrencyCode,
            Status = SupplierInvoiceStatus.Draft,
        };
        var filled = invoice.Fill(order, terms, lines);
        if (filled.IsFailure)
            return filled.Error;
        invoice.Record(PurchasingDocumentAction.Created, userId, nowUtc, note: null);
        return invoice;
    }

    /// <summary>Replaces a draft's supplier invoice number, dates and lines; its order stays.</summary>
    public Result ReplaceDraft(PurchaseOrder order, InvoiceTerms terms, IReadOnlyList<InvoiceLineInput> lines)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (Status != SupplierInvoiceStatus.Draft)
            return SupplierInvoiceDomainErrors.NotDraft;
        return Fill(order, terms, lines);
    }

    /// <summary>Sends a draft with price differences to the approvers.</summary>
    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != SupplierInvoiceStatus.Draft)
            return SupplierInvoiceDomainErrors.NotDraft;
        if (!RequiresApproval)
            return SupplierInvoiceDomainErrors.ApprovalNotRequired;

        Status = SupplierInvoiceStatus.Submitted;
        SubmittedByUserId = userId;
        Record(PurchasingDocumentAction.Submitted, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>
    /// Whether it may post now: directly, when it is a draft without price differences; or by approval, when it is
    /// submitted (whoever entered or submitted it may approve it).
    /// </summary>
    public Result CanPost(bool byApproval)
    {
        if (byApproval)
            return Status == SupplierInvoiceStatus.Submitted ? Result.Success() : SupplierInvoiceDomainErrors.NotSubmitted;
        if (Status != SupplierInvoiceStatus.Draft)
            return SupplierInvoiceDomainErrors.NotDraft;
        return RequiresApproval ? SupplierInvoiceDomainErrors.ApprovalRequired : Result.Success();
    }

    /// <summary>Posts it under its company number; the caller recorded its lines on the order in the same transaction.</summary>
    public void MarkPosted(DocumentNumber number, bool byApproval, Guid? userId, DateTime nowUtc)
    {
        var allowed = CanPost(byApproval);
        if (allowed.IsFailure)
            throw new InvalidOperationException($"Invoice {Id} cannot be posted: {allowed.Error}");

        if (byApproval)
        {
            ApprovedByUserId = userId;
            Record(PurchasingDocumentAction.Approved, userId, nowUtc, note: null);
        }
        Status = SupplierInvoiceStatus.Posted;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        PostedAtUtc = nowUtc;
        PostedByUserId = userId;
        Record(PurchasingDocumentAction.Posted, userId, nowUtc, note: null);
    }

    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != SupplierInvoiceStatus.Submitted)
            return SupplierInvoiceDomainErrors.NotSubmitted;

        Status = SupplierInvoiceStatus.Draft;
        SubmittedByUserId = null;
        Record(PurchasingDocumentAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status is not (SupplierInvoiceStatus.Draft or SupplierInvoiceStatus.Submitted))
            return SupplierInvoiceDomainErrors.NotOpen;

        Status = SupplierInvoiceStatus.Cancelled;
        Record(PurchasingDocumentAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>What each line bills, for the order to record when the invoice posts.</summary>
    public IReadOnlyList<InvoiceMatch> Matches() =>
        _lines.OrderBy(l => l.LineNumber)
            .Select(l => new InvoiceMatch(l.PurchaseOrderLineId, l.Quantity, l.NetAmount, l.TaxAmount))
            .ToList();

    private Result Fill(PurchaseOrder order, InvoiceTerms terms, IReadOnlyList<InvoiceLineInput> lines)
    {
        ArgumentNullException.ThrowIfNull(terms);
        if (lines.Count == 0)
            return SupplierInvoiceDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return SupplierInvoiceDomainErrors.TooManyLines;
        if (lines.Select(l => l.OrderLineId).Distinct().Count() != lines.Count)
            return SupplierInvoiceDomainErrors.DuplicateLine;
        var key = SupplierInvoiceNumbers.Key(terms.SupplierInvoiceNumber);
        if (key is null)
            return SupplierInvoiceDomainErrors.InvoiceNumberInvalid;
        var dueDate = terms.DueDate ?? terms.InvoiceDate.AddDays(order.PaymentTermDays);
        if (dueDate < terms.InvoiceDate)
            return SupplierInvoiceDomainErrors.DueBeforeInvoice;

        var decimals = Amounts.CurrencyDecimals(CurrencyCode);
        var built = new List<SupplierInvoiceLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var orderLine = order.FindLine(lines[i].OrderLineId);
            if (orderLine is null)
                return SupplierInvoiceDomainErrors.OrderLineNotFound(i + 1);
            var line = SupplierInvoiceLine.Create(i + 1, orderLine, lines[i], decimals);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        SupplierInvoiceNumber = terms.SupplierInvoiceNumber.Trim();
        SupplierInvoiceNumberKey = key;
        InvoiceDate = terms.InvoiceDate;
        DueDate = dueDate;
        Notes = Text.Optional(terms.Notes);
        _lines.Clear();
        _lines.AddRange(built);
        RequiresApproval = built.Any(l => l.HasPriceVariance);
        GrossAmount = built.Sum(l => l.GrossAmount);
        DiscountAmount = built.Sum(l => l.DiscountAmount);
        NetAmount = built.Sum(l => l.NetAmount);
        TaxAmount = built.Sum(l => l.TaxAmount);
        TotalAmount = built.Sum(l => l.TotalAmount);
        PriceVarianceAmount = built.Sum(l => l.PriceVarianceAmount);
        return Result.Success();
    }

    private void Record(PurchasingDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(SupplierInvoiceEvent.Create(action, userId, nowUtc, note));
}
