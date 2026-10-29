using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// An offer to one customer for goods from one physical warehouse (its branch numbers it), in the company's base
/// currency, valid until a date. It reserves nothing. Its lines change only while it is a draft. Submitting issues and
/// numbers it at once, or sends it to an approver when the sales settings ask (policy, discount threshold, manual or
/// below-list prices). An issued quotation is declined by the customer, cancelled, or accepted, which converts it once
/// into a draft sales order with the same lines and prices; past its validity date it can no longer be converted.
/// </summary>
public sealed class SalesQuotation : IAuditable
{
    public const int MaxLines = 200;

    private readonly List<SalesQuotationLine> _lines = [];
    private readonly List<SalesQuotationEvent> _events = [];

    private SalesQuotation() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DQT-000123), from an identity column; gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    public string? Number { get; private set; }
    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }

    /// <summary>The warehouse's branch, which scopes the number series.</summary>
    public Guid BranchId { get; private set; }

    public string CurrencyCode { get; private set; } = null!;
    public SalesQuotationStatus Status { get; private set; }
    public DateOnly QuotationDate { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    public string? CustomerReference { get; private set; }
    public int PaymentTermDays { get; private set; }
    public string? Notes { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    /// <summary>Why it went to an approver (<see cref="Domain.ApprovalReasons"/> flags); 0 when issued directly.</summary>
    public int ApprovalReasonFlags { get; private set; }

    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? IssuedAtUtc { get; private set; }
    public string? DeclinedReason { get; private set; }

    /// <summary>The sales order it became.</summary>
    public Guid? ConvertedOrderId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<SalesQuotationLine> Lines => _lines;
    public IReadOnlyList<SalesQuotationEvent> Events => _events;

    public static Result<SalesQuotation> CreateDraft(
        Guid warehouseId, Guid branchId, string currencyCode, QuotationTerms terms, IReadOnlyList<SalesLineInput> lines,
        Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var quotation = new SalesQuotation
        {
            WarehouseId = warehouseId,
            BranchId = branchId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = SalesQuotationStatus.Draft,
        };
        var filled = quotation.Fill(terms, lines);
        if (filled.IsFailure)
            return filled.Error;
        quotation.Record(SalesDocumentAction.Created, userId, nowUtc, note: null);
        return quotation;
    }

    /// <summary>Replaces a draft's customer, terms and lines; the warehouse stays.</summary>
    public Result ReplaceDraft(QuotationTerms terms, IReadOnlyList<SalesLineInput> lines) =>
        Status != SalesQuotationStatus.Draft ? SalesQuotationDomainErrors.NotDraft : Fill(terms, lines);

    /// <summary>Sends a draft to an approver, saying why it needs one.</summary>
    public Result Submit(IReadOnlyList<ApprovalReason> reasons, Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesQuotationStatus.Draft)
            return SalesQuotationDomainErrors.NotDraft;
        if (reasons.Count == 0)
            throw new ArgumentException("A quotation goes to an approver only for a reason.", nameof(reasons));

        Status = SalesQuotationStatus.Submitted;
        SubmittedByUserId = userId;
        ApprovalReasonFlags = ApprovalReasons.ToFlags(reasons);
        Record(SalesDocumentAction.Submitted, userId, nowUtc, ApprovalReasons.Note(reasons));
        return Result.Success();
    }

    /// <summary>Issues a draft that needs no approval: numbered at once.</summary>
    public Result IssueDirectly(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesQuotationStatus.Draft)
            return SalesQuotationDomainErrors.NotDraft;

        SubmittedByUserId = userId;
        ApprovalReasonFlags = 0;
        MarkIssued(number, userId, nowUtc);
        return Result.Success();
    }

    /// <summary>Only a submitted quotation is approved. Whoever approves may have created or submitted it (CLAUDE.md).</summary>
    public Result CanApprove() =>
        Status == SalesQuotationStatus.Submitted ? Result.Success() : SalesQuotationDomainErrors.NotSubmitted;

    /// <summary>Approving issues and numbers it.</summary>
    public Result Approve(DocumentNumber number, Guid? approverId, DateTime nowUtc)
    {
        var allowed = CanApprove();
        if (allowed.IsFailure)
            return allowed;
        ApprovedByUserId = approverId;
        ApprovedAtUtc = nowUtc;
        Record(SalesDocumentAction.Approved, approverId, nowUtc, note: null);
        MarkIssued(number, approverId, nowUtc);
        return Result.Success();
    }

    /// <summary>Sends a submitted quotation back to draft, saying why.</summary>
    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != SalesQuotationStatus.Submitted)
            return SalesQuotationDomainErrors.NotSubmitted;

        Status = SalesQuotationStatus.Draft;
        SubmittedByUserId = null;
        ApprovalReasonFlags = 0;
        Record(SalesDocumentAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    /// <summary>The customer declined an issued quotation.</summary>
    public Result Decline(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != SalesQuotationStatus.Issued)
            return SalesQuotationDomainErrors.NotIssued;

        Status = SalesQuotationStatus.Declined;
        DeclinedReason = reason.Trim();
        Record(SalesDocumentAction.Declined, userId, nowUtc, DeclinedReason);
        return Result.Success();
    }

    /// <summary>Abandons it before the customer accepted it.</summary>
    public Result Cancel(string? reason, Guid? userId, DateTime nowUtc)
    {
        if (Status is not (SalesQuotationStatus.Draft or SalesQuotationStatus.Submitted or SalesQuotationStatus.Issued))
            return SalesQuotationDomainErrors.NotCancellable;

        Status = SalesQuotationStatus.Cancelled;
        Record(SalesDocumentAction.Cancelled, userId, nowUtc, Text.Optional(reason));
        return Result.Success();
    }

    /// <summary>Whether the customer may still accept it on this business date.</summary>
    public Result CanConvert(DateOnly today)
    {
        if (Status != SalesQuotationStatus.Issued)
            return SalesQuotationDomainErrors.NotIssued;
        return IsExpiredOn(today) ? SalesQuotationDomainErrors.Expired : Result.Success();
    }

    /// <summary>The customer accepted it: it became this sales order, once.</summary>
    public Result MarkConverted(Guid orderId, DateOnly today, Guid? userId, DateTime nowUtc)
    {
        var allowed = CanConvert(today);
        if (allowed.IsFailure)
            return allowed;
        Status = SalesQuotationStatus.Converted;
        ConvertedOrderId = orderId;
        Record(SalesDocumentAction.Converted, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Issued and past its validity date.</summary>
    public bool IsExpiredOn(DateOnly today) => Status == SalesQuotationStatus.Issued && ValidUntil < today;

    private void MarkIssued(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        Status = SalesQuotationStatus.Issued;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        IssuedAtUtc = nowUtc;
        Record(SalesDocumentAction.Issued, userId, nowUtc, note: null);
    }

    private Result Fill(QuotationTerms terms, IReadOnlyList<SalesLineInput> lines)
    {
        ArgumentNullException.ThrowIfNull(terms);
        if (lines.Count == 0)
            return SalesQuotationDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return SalesQuotationDomainErrors.TooManyLines;
        if (terms.ValidUntil < terms.QuotationDate)
            return SalesQuotationDomainErrors.ValidityInvalid;
        if (terms.PaymentTermDays is < 0 or > 365)
            return SalesQuotationDomainErrors.PaymentTermsInvalid;

        var decimals = Amounts.CurrencyDecimals(CurrencyCode);
        var built = new List<SalesQuotationLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = SalesQuotationLine.Create(i + 1, lines[i], decimals);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        CustomerId = terms.CustomerId;
        QuotationDate = terms.QuotationDate;
        ValidUntil = terms.ValidUntil;
        CustomerReference = Text.Optional(terms.CustomerReference);
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

    private void Record(SalesDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(SalesQuotationEvent.Create(action, userId, nowUtc, note));
}
