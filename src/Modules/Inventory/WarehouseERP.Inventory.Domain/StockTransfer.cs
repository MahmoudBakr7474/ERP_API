using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// Stock moving from one physical warehouse to another through a transit warehouse. Dispatch moves every line into a
/// transit location opened for this transfer; receipts move part or all of it on to the destination; what never arrives
/// is written off by an approved loss adjustment from that transit location. The ledger documents (dispatch, receipts,
/// the loss adjustment) point back here; the transfer itself only tracks progress.
/// </summary>
public sealed class StockTransfer : IAuditable
{
    public const int MaxLines = 500;

    private readonly List<StockTransferLine> _lines = [];
    private readonly List<StockTransferEvent> _events = [];

    private StockTransfer() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of its reference (TR-000123) before dispatch gives it a transfer number; gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    /// <summary>The dispatch document's number (TRF-…), from dispatch on.</summary>
    public string? Number { get; private set; }

    public StockTransferStatus Status { get; private set; }
    public Guid SourceWarehouseId { get; private set; }
    public Guid SourceBranchId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }
    public Guid DestinationBranchId { get; private set; }
    public Guid TransitWarehouseId { get; private set; }

    /// <summary>The location opened for this transfer in the transit warehouse, from dispatch on.</summary>
    public Guid? TransitLocationId { get; private set; }

    /// <summary>The dispatch approval policy when the transfer was created.</summary>
    public bool RequiresApproval { get; private set; }

    public string? ExternalReference { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? DispatchedAtUtc { get; private set; }
    public Guid? DispatchedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<StockTransferLine> Lines => _lines;
    public IReadOnlyList<StockTransferEvent> Events => _events;

    /// <summary>Stock is on its way or still expected: the warehouses it touches have an open obligation.</summary>
    public bool IsOpen => Status is StockTransferStatus.Submitted or StockTransferStatus.InTransit
        or StockTransferStatus.PartiallyReceived or StockTransferStatus.Closing;

    public decimal OutstandingQuantity => _lines.Sum(l => l.OutstandingQuantity);

    public static Result<StockTransfer> CreateDraft(
        Guid sourceWarehouseId, Guid sourceBranchId, Guid destinationWarehouseId, Guid destinationBranchId,
        Guid transitWarehouseId, bool requiresApproval, string? externalReference, string? notes,
        IReadOnlyList<TransferLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        if (sourceWarehouseId == destinationWarehouseId)
            return StockTransferDomainErrors.SameWarehouse;

        var transfer = new StockTransfer
        {
            Status = StockTransferStatus.Draft,
            SourceWarehouseId = sourceWarehouseId,
            SourceBranchId = sourceBranchId,
            DestinationWarehouseId = destinationWarehouseId,
            DestinationBranchId = destinationBranchId,
            RequiresApproval = requiresApproval,
        };
        var filled = transfer.Fill(transitWarehouseId, externalReference, notes, lines);
        if (filled.IsFailure)
            return filled.Error;

        transfer.Record(StockTransferAction.Created, userId, nowUtc, note: null);
        return transfer;
    }

    /// <summary>Replaces a draft's transit warehouse, references and lines; source and destination are fixed.</summary>
    public Result ReplaceDraft(Guid transitWarehouseId, string? externalReference, string? notes, IReadOnlyList<TransferLineInput> lines)
    {
        if (Status != StockTransferStatus.Draft)
            return StockTransferDomainErrors.NotDraft;
        return Fill(transitWarehouseId, externalReference, notes, lines);
    }

    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != StockTransferStatus.Draft)
            return StockTransferDomainErrors.NotDraft;
        if (!RequiresApproval)
            return StockTransferDomainErrors.ApprovalNotRequired;

        Status = StockTransferStatus.Submitted;
        Record(StockTransferAction.Submitted, userId, nowUtc, note: null);
        return Result.Success();
    }

    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != StockTransferStatus.Submitted)
            return StockTransferDomainErrors.NotSubmitted;

        Status = StockTransferStatus.Draft;
        Record(StockTransferAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status is not (StockTransferStatus.Draft or StockTransferStatus.Submitted))
            return StockTransferDomainErrors.NotOpen;

        Status = StockTransferStatus.Cancelled;
        Record(StockTransferAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>
    /// Whether it may be dispatched now: directly, when it needs no approval and is a draft; or by approval, when it is
    /// submitted and the approver did not create it.
    /// </summary>
    public Result CanDispatch(bool byApproval, Guid? userId)
    {
        if (byApproval)
        {
            if (Status != StockTransferStatus.Submitted)
                return StockTransferDomainErrors.NotSubmitted;
            if (userId is null || userId == CreatedByUserId)
                return StockTransferDomainErrors.SelfApproval;
            return Result.Success();
        }

        if (RequiresApproval)
            return StockTransferDomainErrors.ApprovalRequired;
        if (Status != StockTransferStatus.Draft)
            return StockTransferDomainErrors.NotDraft;
        return Result.Success();
    }

    /// <summary>Records the dispatch the caller posted: the stock now sits in the transfer's transit location.</summary>
    public void MarkDispatched(string number, Guid transitLocationId, bool byApproval, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        var allowed = CanDispatch(byApproval, userId);
        if (allowed.IsFailure)
            throw new InvalidOperationException($"Transfer {Id} cannot be dispatched: {allowed.Error}");

        if (byApproval)
        {
            ApprovedByUserId = userId;
            Record(StockTransferAction.Approved, userId, nowUtc, note: null);
        }
        Number = number;
        TransitLocationId = transitLocationId;
        Status = StockTransferStatus.InTransit;
        DispatchedAtUtc = nowUtc;
        DispatchedByUserId = userId;
        Record(StockTransferAction.Dispatched, userId, nowUtc, number);
    }

    /// <summary>Whether this much of these lines may arrive: dispatched, not being closed, and never more than is in transit.</summary>
    public Result CheckReceipt(IReadOnlyList<TransferReceiptInput> receipts)
    {
        if (Status is not (StockTransferStatus.InTransit or StockTransferStatus.PartiallyReceived))
            return StockTransferDomainErrors.NotInTransit;
        if (receipts.Count == 0)
            return StockTransferDomainErrors.NothingToReceive;

        foreach (var group in receipts.GroupBy(r => r.LineId))
        {
            var line = _lines.SingleOrDefault(l => l.Id == group.Key);
            if (line is null)
                return StockTransferDomainErrors.UnknownLine(group.Key);
            foreach (var receipt in group)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receipt.Quantity);
                if (!receipt.BaseUnitAllowsFraction && !Quantities.IsWhole(receipt.Quantity))
                    return StockTransferDomainErrors.QuantityMustBeWhole(line.LineNumber);
            }
            if (group.Sum(r => r.Quantity) > line.OutstandingQuantity)
                return StockTransferDomainErrors.ReceiptExceedsOutstanding(line.LineNumber);
        }
        return Result.Success();
    }

    /// <summary>Records a receipt the caller posted; the transfer is received once nothing is left in transit.</summary>
    public void RecordReceipt(IReadOnlyList<TransferReceiptInput> receipts, string documentNumber, Guid? userId, DateTime nowUtc)
    {
        var allowed = CheckReceipt(receipts);
        if (allowed.IsFailure)
            throw new InvalidOperationException($"Transfer {Id} cannot receive this: {allowed.Error}");

        foreach (var receipt in receipts)
            _lines.Single(l => l.Id == receipt.LineId).Receive(receipt.Quantity);
        if (_lines.All(l => l.OutstandingQuantity == 0))
        {
            Status = StockTransferStatus.Received;
            CompletedAtUtc = nowUtc;
        }
        else
            Status = StockTransferStatus.PartiallyReceived;
        Record(StockTransferAction.Received, userId, nowUtc, documentNumber);
    }

    /// <summary>Stops receiving while the outstanding quantity waits for an approved loss adjustment.</summary>
    public Result BeginClosing(string? note, Guid? userId, DateTime nowUtc)
    {
        if (Status is not (StockTransferStatus.InTransit or StockTransferStatus.PartiallyReceived))
            return StockTransferDomainErrors.NotInTransit;
        if (OutstandingQuantity == 0)
            return StockTransferDomainErrors.NothingOutstanding;

        Status = StockTransferStatus.Closing;
        Record(StockTransferAction.ClosingRequested, userId, nowUtc, Text.Optional(note));
        return Result.Success();
    }

    /// <summary>The loss adjustment was cancelled: receiving goes on.</summary>
    public void AbortClosing(Guid? userId, DateTime nowUtc)
    {
        if (Status != StockTransferStatus.Closing)
            throw new InvalidOperationException($"Transfer {Id} is not being closed.");
        Status = _lines.Any(l => l.ReceivedQuantity > 0) ? StockTransferStatus.PartiallyReceived : StockTransferStatus.InTransit;
        Record(StockTransferAction.ClosingAborted, userId, nowUtc, note: null);
    }

    /// <summary>The loss adjustment posted: what was outstanding is written off and the transfer is closed.</summary>
    public void CompleteClosing(string adjustmentNumber, Guid? userId, DateTime nowUtc)
    {
        if (Status != StockTransferStatus.Closing)
            throw new InvalidOperationException($"Transfer {Id} is not being closed.");
        foreach (var line in _lines)
            line.WriteOffOutstanding();
        Status = StockTransferStatus.Closed;
        CompletedAtUtc = nowUtc;
        Record(StockTransferAction.Closed, userId, nowUtc, adjustmentNumber);
    }

    private Result Fill(Guid transitWarehouseId, string? externalReference, string? notes, IReadOnlyList<TransferLineInput> lines)
    {
        if (lines.Count == 0)
            return StockTransferDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return StockTransferDomainErrors.TooManyLines;

        var built = new List<StockTransferLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = StockTransferLine.Create(SourceWarehouseId, i + 1, lines[i]);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        TransitWarehouseId = transitWarehouseId;
        ExternalReference = Text.Optional(externalReference);
        Notes = Text.Optional(notes);
        _lines.Clear();
        _lines.AddRange(built);
        return Result.Success();
    }

    private void Record(StockTransferAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(StockTransferEvent.Create(action, userId, nowUtc, note));
}
