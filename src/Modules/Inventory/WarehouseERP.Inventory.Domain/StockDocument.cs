using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// A stock document for one physical warehouse: opening stock, goods receipt, goods issue or adjustment, or the
/// reversal of a posted one. Its lines change only while it is a draft; posting numbers it and makes it immutable.
/// </summary>
public sealed class StockDocument : IAuditable
{
    public const int MaxLines = 500;

    private readonly List<StockDocumentLine> _lines = [];
    private readonly List<StockDocumentEvent> _events = [];

    private StockDocument() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DR-000123), from a database sequence; gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    public StockDocumentType DocumentType { get; private set; }
    public StockDocumentStatus Status { get; private set; }
    public Guid WarehouseId { get; private set; }

    /// <summary>The warehouse's branch, which scopes the number series.</summary>
    public Guid BranchId { get; private set; }

    /// <summary>The approval policy when the document was created; later policy changes leave it alone.</summary>
    public bool RequiresApproval { get; private set; }

    public Guid? ReversalOfDocumentId { get; private set; }
    public AdjustmentReason? AdjustmentReason { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>The final number, given only when posting succeeds and kept forever.</summary>
    public string? Number { get; private set; }

    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<StockDocumentLine> Lines => _lines;
    public IReadOnlyList<StockDocumentEvent> Events => _events;

    public static Result<StockDocument> CreateDraft(
        StockDocumentType documentType, Guid warehouseId, Guid branchId, bool requiresApproval, AdjustmentReason? reason,
        string? externalReference, string? notes, IReadOnlyList<StockLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        if (documentType == StockDocumentType.Reversal)
            throw new ArgumentException("A reversal is created from the posted document it reverses.", nameof(documentType));

        var document = new StockDocument
        {
            DocumentType = documentType,
            Status = StockDocumentStatus.Draft,
            WarehouseId = warehouseId,
            BranchId = branchId,
            RequiresApproval = requiresApproval,
        };
        var filled = document.Fill(reason, externalReference, notes, lines);
        if (filled.IsFailure)
            return filled.Error;

        document.Record(StockDocumentAction.Created, userId, nowUtc, note: null);
        return document;
    }

    /// <summary>Replaces a draft's reason, references and lines.</summary>
    public Result ReplaceDraft(AdjustmentReason? reason, string? externalReference, string? notes, IReadOnlyList<StockLineInput> lines)
    {
        if (DocumentType == StockDocumentType.Reversal)
            return StockDocumentDomainErrors.ReversalLinesFixed;
        if (Status != StockDocumentStatus.Draft)
            return StockDocumentDomainErrors.NotDraft;
        return Fill(reason, externalReference, notes, lines);
    }

    /// <summary>
    /// The reversal of a posted document: the same lines the other way round, submitted for approval at once. Stock that
    /// came in goes out at the average cost; stock that went out comes back at the value it left with.
    /// </summary>
    public static Result<StockDocument> CreateReversal(StockDocument original, string? notes, Guid? userId, DateTime nowUtc)
    {
        if (original.DocumentType == StockDocumentType.Reversal)
            return StockDocumentDomainErrors.CannotReverseReversal;
        if (original.Status != StockDocumentStatus.Posted)
            return StockDocumentDomainErrors.NotPosted;

        var reversal = new StockDocument
        {
            DocumentType = StockDocumentType.Reversal,
            Status = StockDocumentStatus.Submitted,
            WarehouseId = original.WarehouseId,
            BranchId = original.BranchId,
            RequiresApproval = true,
            ReversalOfDocumentId = original.Id,
            ExternalReference = original.Number,
            Notes = Text.Optional(notes),
        };
        foreach (var line in original.Lines)
            reversal._lines.Add(line.Reversed());
        reversal.Record(StockDocumentAction.Created, userId, nowUtc, note: null);
        reversal.Record(StockDocumentAction.Submitted, userId, nowUtc, note: null);
        return reversal;
    }

    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != StockDocumentStatus.Draft)
            return StockDocumentDomainErrors.NotDraft;
        if (!RequiresApproval)
            return StockDocumentDomainErrors.ApprovalNotRequired;

        Status = StockDocumentStatus.Submitted;
        Record(StockDocumentAction.Submitted, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Sends a submitted document back to draft, saying why.</summary>
    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != StockDocumentStatus.Submitted)
            return StockDocumentDomainErrors.NotSubmitted;

        Status = StockDocumentStatus.Draft;
        Record(StockDocumentAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status is not (StockDocumentStatus.Draft or StockDocumentStatus.Submitted))
            return StockDocumentDomainErrors.NotOpen;

        Status = StockDocumentStatus.Cancelled;
        Record(StockDocumentAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>
    /// Whether it may be posted now: directly, when it needs no approval and is a draft; or by approval, when it is
    /// submitted and the approver did not create it.
    /// </summary>
    public Result CanPost(bool byApproval, Guid? userId)
    {
        if (byApproval)
        {
            if (Status != StockDocumentStatus.Submitted)
                return StockDocumentDomainErrors.NotSubmitted;
            if (userId is null || userId == CreatedByUserId)
                return StockDocumentDomainErrors.SelfApproval;
            return Result.Success();
        }

        if (RequiresApproval)
            return StockDocumentDomainErrors.ApprovalRequired;
        if (Status != StockDocumentStatus.Draft)
            return StockDocumentDomainErrors.NotDraft;
        return Result.Success();
    }

    /// <summary>Posts it under its final number. The caller has applied its stock and valuation in the same transaction.</summary>
    public void MarkPosted(string number, short fiscalYear, int sequenceNumber, bool byApproval, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        var allowed = CanPost(byApproval, userId);
        if (allowed.IsFailure)
            throw new InvalidOperationException($"Document {Id} cannot be posted: {allowed.Error}");

        if (byApproval)
        {
            ApprovedByUserId = userId;
            Record(StockDocumentAction.Approved, userId, nowUtc, note: null);
        }
        Status = StockDocumentStatus.Posted;
        Number = number;
        FiscalYear = fiscalYear;
        SequenceNumber = sequenceNumber;
        PostedAtUtc = nowUtc;
        PostedByUserId = userId;
        Record(StockDocumentAction.Posted, userId, nowUtc, note: null);
    }

    private Result Fill(AdjustmentReason? reason, string? externalReference, string? notes, IReadOnlyList<StockLineInput> lines)
    {
        if (lines.Count == 0)
            return StockDocumentDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return StockDocumentDomainErrors.TooManyLines;
        if (DocumentType == StockDocumentType.Adjustment && reason is null)
            return StockDocumentDomainErrors.ReasonRequired;
        if (DocumentType != StockDocumentType.Adjustment && reason is not null)
            return StockDocumentDomainErrors.ReasonOnlyForAdjustments;

        var built = new List<StockDocumentLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = StockDocumentLine.Create(DocumentType, WarehouseId, i + 1, lines[i]);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        AdjustmentReason = reason;
        ExternalReference = Text.Optional(externalReference);
        Notes = Text.Optional(notes);
        _lines.Clear();
        _lines.AddRange(built);
        return Result.Success();
    }

    private void Record(StockDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(StockDocumentEvent.Create(action, userId, nowUtc, note));
}
