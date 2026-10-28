using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// Goods of one posted goods receipt sent back to the supplier, with a reason. It always needs approval, which its creator
/// may give when they hold the permission. Approving posts it: Inventory issues the goods through a PurchaseReturn stock
/// document at the current moving average, the receipt and order lines record what went back, and each line keeps how
/// much had not been invoiced yet (it will not be) and how much had (a pending credit claim on the supplier, valued at
/// the invoiced price). The claim is information for future Accounting, not a posted credit note.
/// </summary>
public sealed class SupplierReturn : IAuditable
{
    public const int MaxLines = 200;

    private readonly List<SupplierReturnLine> _lines = [];
    private readonly List<SupplierReturnEvent> _events = [];

    private SupplierReturn() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DSRN-000123), from an identity column.</summary>
    public int DraftNumber { get; private set; }

    public string? Number { get; private set; }
    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid GoodsReceiptId { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public string Reason { get; private set; } = null!;
    public string? Notes { get; private set; }
    public SupplierReturnStatus Status { get; private set; }

    /// <summary>The PurchaseReturn stock document Inventory posted for it.</summary>
    public Guid? StockDocumentId { get; private set; }

    public string? StockDocumentNumber { get; private set; }

    /// <summary>Net amount the supplier owes back for returned goods that were already invoiced.</summary>
    public decimal CreditClaimNetAmount { get; private set; }

    public decimal CreditClaimTaxAmount { get; private set; }
    public decimal CreditClaimTotalAmount { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<SupplierReturnLine> Lines => _lines;
    public IReadOnlyList<SupplierReturnEvent> Events => _events;

    public static Result<SupplierReturn> CreateDraft(
        GoodsReceipt receipt, string currencyCode, string reason, string? notes, IReadOnlyList<ReturnLineInput> lines,
        Guid? userId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var supplierReturn = new SupplierReturn
        {
            GoodsReceiptId = receipt.Id,
            PurchaseOrderId = receipt.PurchaseOrderId,
            SupplierId = receipt.SupplierId,
            WarehouseId = receipt.WarehouseId,
            BranchId = receipt.BranchId,
            CurrencyCode = currencyCode,
            Status = SupplierReturnStatus.Draft,
        };
        var filled = supplierReturn.Fill(receipt, reason, notes, lines);
        if (filled.IsFailure)
            return filled.Error;
        supplierReturn.Record(PurchasingDocumentAction.Created, userId, nowUtc, note: null);
        return supplierReturn;
    }

    public Result ReplaceDraft(GoodsReceipt receipt, string reason, string? notes, IReadOnlyList<ReturnLineInput> lines)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (Status != SupplierReturnStatus.Draft)
            return SupplierReturnDomainErrors.NotDraft;
        return Fill(receipt, reason, notes, lines);
    }

    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != SupplierReturnStatus.Draft)
            return SupplierReturnDomainErrors.NotDraft;

        Status = SupplierReturnStatus.Submitted;
        SubmittedByUserId = userId;
        Record(PurchasingDocumentAction.Submitted, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Only a submitted return is approved; its creator may approve it.</summary>
    public Result CanApprove() =>
        Status == SupplierReturnStatus.Submitted ? Result.Success() : SupplierReturnDomainErrors.NotSubmitted;

    /// <summary>
    /// Posts it: numbered, linked to the stock document Inventory posted, each line with its invoiced and uninvoiced split
    /// (in line order) and the pending credit claim they add up to.
    /// </summary>
    public void MarkPosted(
        DocumentNumber number, IReadOnlyList<ReturnSplit> splits, Guid stockDocumentId, string stockDocumentNumber, Guid? approverId,
        DateTime nowUtc)
    {
        var allowed = CanApprove();
        if (allowed.IsFailure)
            throw new InvalidOperationException($"Return {Id} cannot be posted: {allowed.Error}");
        if (splits.Count != _lines.Count)
            throw new ArgumentException("One split per line.", nameof(splits));

        var ordered = _lines.OrderBy(l => l.LineNumber).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].ApplySplit(splits[i]);
        CreditClaimNetAmount = ordered.Sum(l => l.CreditClaimNetAmount);
        CreditClaimTaxAmount = ordered.Sum(l => l.CreditClaimTaxAmount);
        CreditClaimTotalAmount = CreditClaimNetAmount + CreditClaimTaxAmount;
        Status = SupplierReturnStatus.Posted;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        StockDocumentId = stockDocumentId;
        StockDocumentNumber = stockDocumentNumber;
        ApprovedByUserId = approverId;
        PostedAtUtc = nowUtc;
        Record(PurchasingDocumentAction.Approved, approverId, nowUtc, note: null);
        Record(PurchasingDocumentAction.Posted, approverId, nowUtc, note: null);
    }

    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != SupplierReturnStatus.Submitted)
            return SupplierReturnDomainErrors.NotSubmitted;

        Status = SupplierReturnStatus.Draft;
        SubmittedByUserId = null;
        Record(PurchasingDocumentAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status is not (SupplierReturnStatus.Draft or SupplierReturnStatus.Submitted))
            return SupplierReturnDomainErrors.NotOpen;

        Status = SupplierReturnStatus.Cancelled;
        Record(PurchasingDocumentAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    private Result Fill(GoodsReceipt receipt, string reason, string? notes, IReadOnlyList<ReturnLineInput> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (lines.Count == 0)
            return SupplierReturnDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return SupplierReturnDomainErrors.TooManyLines;
        if (lines.Select(l => l.ReceiptLineId).Distinct().Count() != lines.Count)
            return SupplierReturnDomainErrors.DuplicateLine;

        var built = new List<SupplierReturnLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var receiptLine = receipt.FindLine(lines[i].ReceiptLineId);
            if (receiptLine is null)
                return SupplierReturnDomainErrors.ReceiptLineNotFound(i + 1);
            var line = SupplierReturnLine.Create(i + 1, receiptLine, lines[i]);
            if (line.IsFailure)
                return line.Error;
            built.Add(line.Value);
        }

        Reason = reason.Trim();
        Notes = Text.Optional(notes);
        _lines.Clear();
        _lines.AddRange(built);
        return Result.Success();
    }

    private void Record(PurchasingDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(SupplierReturnEvent.Create(action, userId, nowUtc, note));
}
