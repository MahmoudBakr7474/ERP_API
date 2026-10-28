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

    /// <summary>The transfer this document dispatched, received or wrote off.</summary>
    public Guid? TransferId { get; private set; }

    /// <summary>The count whose differences this document posted.</summary>
    public Guid? CountId { get; private set; }

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
        if (TransferId is not null || CountId is not null)
            return StockDocumentDomainErrors.LinkedLinesFixed;
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
        if (original.TransferId is not null || original.CountId is not null
            || original.DocumentType is StockDocumentType.TransferDispatch or StockDocumentType.TransferReceipt or StockDocumentType.CountAdjustment)
            return StockDocumentDomainErrors.NotReversible;
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

    /// <summary>
    /// The document that dispatches a transfer: every line moves from its source location into the transit location
    /// opened for the transfer. Posted at once in the caller's transaction.
    /// </summary>
    public static StockDocument CreateTransferDispatch(StockTransfer transfer, Guid transitLocationId, Guid? userId, DateTime nowUtc)
    {
        var document = Generated(StockDocumentType.TransferDispatch, transfer.SourceWarehouseId, transfer.SourceBranchId,
            transfer.RequiresApproval, StockDocumentStatus.Draft, StockDocumentNumbers.TransferReference(transfer.DraftNumber), transfer.Notes);
        document.TransferId = transfer.Id;
        var transit = new StockPlace(transfer.TransitWarehouseId, transitLocationId);
        foreach (var line in transfer.Lines.OrderBy(l => l.LineNumber))
            document._lines.Add(StockDocumentLine.CreateMove(
                line.LineNumber, line.ProductId, line.ProductUnitId, line.UnitId, line.ConversionFactor, line.Quantity,
                line.BaseQuantity, new StockPlace(transfer.SourceWarehouseId, line.SourceLocationId), transit));
        document.Record(StockDocumentAction.Created, userId, nowUtc, note: null);
        return document;
    }

    /// <summary>
    /// The document that receives part or all of a dispatched transfer: each receipt moves base units from the transit
    /// location into a location of the destination warehouse. Posted at once in the caller's transaction.
    /// </summary>
    public static StockDocument CreateTransferReceipt(
        StockTransfer transfer, IReadOnlyList<TransferReceiptInput> receipts, Guid? userId, DateTime nowUtc)
    {
        if (transfer.TransitLocationId is not { } transitLocationId)
            throw new InvalidOperationException($"Transfer {transfer.Id} was not dispatched.");

        var document = Generated(StockDocumentType.TransferReceipt, transfer.DestinationWarehouseId, transfer.DestinationBranchId,
            requiresApproval: false, StockDocumentStatus.Draft, transfer.Number, notes: null);
        document.TransferId = transfer.Id;
        var transit = new StockPlace(transfer.TransitWarehouseId, transitLocationId);
        for (var i = 0; i < receipts.Count; i++)
        {
            var receipt = receipts[i];
            var line = transfer.Lines.Single(l => l.Id == receipt.LineId);
            document._lines.Add(StockDocumentLine.CreateMove(
                i + 1, line.ProductId, receipt.BaseProductUnitId, receipt.BaseUnitId, 1m, receipt.Quantity, receipt.Quantity,
                transit, new StockPlace(transfer.DestinationWarehouseId, receipt.LocationId)));
        }
        document.Record(StockDocumentAction.Created, userId, nowUtc, note: null);
        return document;
    }

    /// <summary>
    /// The adjustment that writes off what never arrived, out of the transfer's transit location. It is submitted at
    /// once and needs approval like any adjustment; posting it closes the transfer.
    /// </summary>
    public static StockDocument CreateTransferLoss(
        StockTransfer transfer, Guid transitBranchId, AdjustmentReason reason, string? notes,
        IReadOnlyList<GeneratedLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        if (lines.Count == 0)
            throw new ArgumentException("A loss adjustment writes something off.", nameof(lines));

        var document = Generated(StockDocumentType.Adjustment, transfer.TransitWarehouseId, transitBranchId,
            requiresApproval: true, StockDocumentStatus.Submitted, transfer.Number, notes);
        document.TransferId = transfer.Id;
        document.AdjustmentReason = reason;
        for (var i = 0; i < lines.Count; i++)
            document._lines.Add(StockDocumentLine.CreateGenerated(i + 1, lines[i]));
        document.Record(StockDocumentAction.Created, userId, nowUtc, note: null);
        document.Record(StockDocumentAction.Submitted, userId, nowUtc, note: null);
        return document;
    }

    /// <summary>The adjustment an approved count posts for its differences; the count carries the approval.</summary>
    public static StockDocument CreateCountAdjustment(StockCount count, IReadOnlyList<GeneratedLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        if (lines.Count == 0)
            throw new ArgumentException("A count adjustment posts at least one difference.", nameof(lines));

        var document = Generated(StockDocumentType.CountAdjustment, count.WarehouseId, count.BranchId, requiresApproval: true,
            StockDocumentStatus.Submitted, StockDocumentNumbers.CountReference(count.DraftNumber), count.Notes);
        document.CountId = count.Id;
        for (var i = 0; i < lines.Count; i++)
            document._lines.Add(StockDocumentLine.CreateGenerated(i + 1, lines[i]));
        document.Record(StockDocumentAction.Created, userId, nowUtc, note: null);
        return document;
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

    /// <summary>
    /// Posts a document a transfer or a count produced. Its approval, and the rule that nobody approves their own work,
    /// belong to that transfer or count, which checked them; <paramref name="approvedBy"/> records who approved there.
    /// </summary>
    public void MarkPostedGenerated(string number, short fiscalYear, int sequenceNumber, Guid? approvedBy, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        if (Status is not (StockDocumentStatus.Draft or StockDocumentStatus.Submitted))
            throw new InvalidOperationException($"Document {Id} is already {Status}.");

        if (approvedBy is not null)
        {
            ApprovedByUserId = approvedBy;
            Record(StockDocumentAction.Approved, approvedBy, nowUtc, note: null);
        }
        Status = StockDocumentStatus.Posted;
        Number = number;
        FiscalYear = fiscalYear;
        SequenceNumber = sequenceNumber;
        PostedAtUtc = nowUtc;
        PostedByUserId = userId;
        Record(StockDocumentAction.Posted, userId, nowUtc, note: null);
    }

    private static StockDocument Generated(
        StockDocumentType documentType, Guid warehouseId, Guid branchId, bool requiresApproval, StockDocumentStatus status,
        string? externalReference, string? notes) =>
        new()
        {
            DocumentType = documentType,
            Status = status,
            WarehouseId = warehouseId,
            BranchId = branchId,
            RequiresApproval = requiresApproval,
            ExternalReference = Text.Optional(externalReference),
            Notes = Text.Optional(notes),
        };

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
