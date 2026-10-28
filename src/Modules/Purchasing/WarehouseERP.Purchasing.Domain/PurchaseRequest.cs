using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// A warehouse's request for goods, without a supplier or prices. Optional: orders may also be drafted without one.
/// Once approved (by an approver, its creator included, or at once when its policy needs none), orders of the same warehouse draw on
/// its lines: each line records how much is on order, which never exceeds what was approved, and a cancelled order gives
/// its quantity back. It is Ordered when every line is fully on order, or Closed by hand.
/// </summary>
public sealed class PurchaseRequest : IAuditable
{
    public const int MaxLines = 200;

    private readonly List<PurchaseRequestLine> _lines = [];
    private readonly List<PurchaseRequestEvent> _events = [];

    private PurchaseRequest() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DPR-000123), from an identity column.</summary>
    public int DraftNumber { get; private set; }

    public string? Number { get; private set; }
    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public PurchaseRequestStatus Status { get; private set; }

    /// <summary>The approval policy when the request was created.</summary>
    public bool RequiresApproval { get; private set; }

    public DateOnly? NeededByDate { get; private set; }
    public string? Notes { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string? ClosedReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<PurchaseRequestLine> Lines => _lines;
    public IReadOnlyList<PurchaseRequestEvent> Events => _events;

    public static Result<PurchaseRequest> CreateDraft(
        Guid warehouseId, Guid branchId, bool requiresApproval, DateOnly? neededByDate, string? notes,
        IReadOnlyList<RequestLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        var request = new PurchaseRequest
        {
            WarehouseId = warehouseId,
            BranchId = branchId,
            Status = PurchaseRequestStatus.Draft,
            RequiresApproval = requiresApproval,
        };
        var filled = request.Fill(neededByDate, notes, lines);
        if (filled.IsFailure)
            return filled.Error;
        request.Record(PurchasingDocumentAction.Created, userId, nowUtc, note: null);
        return request;
    }

    public Result ReplaceDraft(DateOnly? neededByDate, string? notes, IReadOnlyList<RequestLineInput> lines) =>
        Status != PurchaseRequestStatus.Draft ? PurchaseRequestDomainErrors.NotDraft : Fill(neededByDate, notes, lines);

    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != PurchaseRequestStatus.Draft)
            return PurchaseRequestDomainErrors.NotDraft;
        if (!RequiresApproval)
            return PurchaseRequestDomainErrors.ApprovalNotRequired;

        Status = PurchaseRequestStatus.Submitted;
        SubmittedByUserId = userId;
        Record(PurchasingDocumentAction.Submitted, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Releases a draft whose policy needs no approval: approved and numbered at once.</summary>
    public Result ApproveDirectly(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        if (Status != PurchaseRequestStatus.Draft)
            return PurchaseRequestDomainErrors.NotDraft;
        if (RequiresApproval)
            return PurchaseRequestDomainErrors.ApprovalRequired;

        SubmittedByUserId = userId;
        MarkApproved(number, userId, nowUtc);
        return Result.Success();
    }

    /// <summary>Only a submitted request is approved; its creator may approve it.</summary>
    public Result CanApprove() =>
        Status == PurchaseRequestStatus.Submitted ? Result.Success() : PurchaseRequestDomainErrors.NotSubmitted;

    public Result Approve(DocumentNumber number, Guid? approverId, DateTime nowUtc)
    {
        var allowed = CanApprove();
        if (allowed.IsFailure)
            return allowed;
        MarkApproved(number, approverId, nowUtc);
        return Result.Success();
    }

    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != PurchaseRequestStatus.Submitted)
            return PurchaseRequestDomainErrors.NotSubmitted;

        Status = PurchaseRequestStatus.Draft;
        SubmittedByUserId = null;
        Record(PurchasingDocumentAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    /// <summary>Abandons a request nothing was ordered from.</summary>
    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status is not (PurchaseRequestStatus.Draft or PurchaseRequestStatus.Submitted or PurchaseRequestStatus.Approved)
            || _lines.Any(l => l.OrderedQuantity > 0))
            return PurchaseRequestDomainErrors.NotCancellable;

        Status = PurchaseRequestStatus.Cancelled;
        Record(PurchasingDocumentAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Closes an approved request: what is not on order yet will never be ordered from it.</summary>
    public Result Close(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != PurchaseRequestStatus.Approved)
            return PurchaseRequestDomainErrors.NotClosable;

        Status = PurchaseRequestStatus.Closed;
        ClosedReason = reason.Trim();
        Record(PurchasingDocumentAction.Closed, userId, nowUtc, ClosedReason);
        return Result.Success();
    }

    /// <summary>
    /// Puts quantities of lines on order (positive) or gives them back (negative, when an order is cancelled or a draft
    /// order drops them). Only an approved request takes new orders; a line never goes above what was approved.
    /// </summary>
    public Result ChangeOrdered(IReadOnlyList<RequestAllocation> changes)
    {
        foreach (var change in changes)
        {
            var line = _lines.SingleOrDefault(l => l.Id == change.RequestLineId);
            if (line is null)
                return PurchaseRequestDomainErrors.LineNotFound(change.OrderLineNumber);
            if (change.Quantity > 0 && Status != PurchaseRequestStatus.Approved)
                return PurchaseRequestDomainErrors.NotOrderable(change.OrderLineNumber);
            if (change.Quantity > line.RemainingQuantity)
                return PurchaseRequestDomainErrors.ExceedsRequest(change.OrderLineNumber, line.RemainingQuantity);
            if (line.OrderedQuantity + change.Quantity < 0)
                throw new InvalidOperationException($"Request line {line.Id} would have less than nothing on order.");
        }

        foreach (var change in changes)
            _lines.Single(l => l.Id == change.RequestLineId).AddOrdered(change.Quantity);
        if (Status is PurchaseRequestStatus.Approved or PurchaseRequestStatus.Ordered)
            Status = _lines.All(l => l.RemainingQuantity == 0) ? PurchaseRequestStatus.Ordered : PurchaseRequestStatus.Approved;
        return Result.Success();
    }

    public PurchaseRequestLine? FindLine(Guid lineId) => _lines.SingleOrDefault(l => l.Id == lineId);

    private void MarkApproved(DocumentNumber number, Guid? userId, DateTime nowUtc)
    {
        Status = PurchaseRequestStatus.Approved;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        ApprovedByUserId = userId;
        Record(PurchasingDocumentAction.Approved, userId, nowUtc, note: null);
    }

    private Result Fill(DateOnly? neededByDate, string? notes, IReadOnlyList<RequestLineInput> lines)
    {
        if (lines.Count == 0)
            return PurchaseRequestDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return PurchaseRequestDomainErrors.TooManyLines;

        var built = new List<PurchaseRequestLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = PurchaseRequestLine.Create(i + 1, lines[i]);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        NeededByDate = neededByDate;
        Notes = Text.Optional(notes);
        _lines.Clear();
        _lines.AddRange(built);
        return Result.Success();
    }

    private void Record(PurchasingDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(PurchaseRequestEvent.Create(action, userId, nowUtc, note));
}
