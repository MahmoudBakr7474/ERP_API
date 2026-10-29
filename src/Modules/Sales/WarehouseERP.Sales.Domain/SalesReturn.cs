using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// Goods a customer sends back from one posted delivery: each line at most what its delivery line still has unreturned,
/// fit for resale (damaged or uninspected goods are refused in V1, never restocked), into a location of the delivery's
/// warehouse. It always needs approval, which its creator may give (CLAUDE.md). Approving numbers it and, in the same
/// transaction, Inventory takes the goods back into sellable stock at the cost their delivery line left with, and the
/// order splits each quantity: first what was never invoiced (it won't be), then invoiced goods, which become a pending
/// customer credit valued at the line's average invoiced price and tax. That credit is no credit note and no refund: it
/// waits for Accounting.
/// </summary>
public sealed class SalesReturn : IAuditable
{
    public const int MaxLines = 500;

    private readonly List<SalesReturnLine> _lines = [];
    private readonly List<SalesReturnEvent> _events = [];

    private SalesReturn() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DSR-000123), from an identity column; gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    public string? Number { get; private set; }
    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid DeliveryId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public SalesReturnStatus Status { get; private set; }

    /// <summary>Why the customer sent the goods back.</summary>
    public string Reason { get; private set; } = null!;

    public string? Notes { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }

    /// <summary>The SalesReturn stock document Inventory posted (it names this return back).</summary>
    public Guid? StockDocumentId { get; private set; }

    public string? StockDocumentNumber { get; private set; }

    /// <summary>What the goods came back at, in the base currency: their delivery's cost, reversing the cost of goods sold.</summary>
    public decimal CostAmount { get; private set; }

    public decimal CreditNetAmount { get; private set; }
    public decimal CreditTaxAmount { get; private set; }

    /// <summary>What the company owes the customer for invoiced goods that came back, pending Accounting.</summary>
    public decimal CreditTotalAmount { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<SalesReturnLine> Lines => _lines;
    public IReadOnlyList<SalesReturnEvent> Events => _events;

    public static Result<SalesReturn> CreateDraft(
        SalesDelivery delivery, string currencyCode, string reason, string? notes, IReadOnlyList<ReturnLineInput> lines,
        Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var salesReturn = new SalesReturn
        {
            DeliveryId = delivery.Id,
            OrderId = delivery.OrderId,
            CustomerId = delivery.CustomerId,
            WarehouseId = delivery.WarehouseId,
            BranchId = delivery.BranchId,
            CurrencyCode = currencyCode,
            Status = SalesReturnStatus.Draft,
        };
        var filled = salesReturn.Fill(delivery, reason, notes, lines);
        if (filled.IsFailure)
            return filled.Error;
        salesReturn.Record(SalesDocumentAction.Created, userId, nowUtc, note: null);
        return salesReturn;
    }

    /// <summary>Replaces a draft's reason, notes and lines.</summary>
    public Result ReplaceDraft(SalesDelivery delivery, string reason, string? notes, IReadOnlyList<ReturnLineInput> lines) =>
        Status != SalesReturnStatus.Draft ? SalesReturnDomainErrors.NotDraft : Fill(delivery, reason, notes, lines);

    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesReturnStatus.Draft)
            return SalesReturnDomainErrors.NotDraft;
        Status = SalesReturnStatus.Submitted;
        SubmittedByUserId = userId;
        Record(SalesDocumentAction.Submitted, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Only a submitted return is approved. Whoever approves may have created or submitted it (CLAUDE.md).</summary>
    public Result CanApprove() =>
        Status == SalesReturnStatus.Submitted ? Result.Success() : SalesReturnDomainErrors.NotSubmitted;

    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != SalesReturnStatus.Submitted)
            return SalesReturnDomainErrors.NotSubmitted;
        Status = SalesReturnStatus.Draft;
        SubmittedByUserId = null;
        Record(SalesDocumentAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status is not (SalesReturnStatus.Draft or SalesReturnStatus.Submitted))
            return SalesReturnDomainErrors.NotCancellable;
        Status = SalesReturnStatus.Cancelled;
        Record(SalesDocumentAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>What it brings back per delivery line, for the delivery.</summary>
    public IReadOnlyList<(Guid DeliveryLineId, decimal Quantity)> DeliveryQuantities() =>
        _lines.OrderBy(l => l.LineNumber).Select(l => (l.DeliveryLineId, l.Quantity)).ToList();

    /// <summary>What it brings back per order line, for the order.</summary>
    public IReadOnlyList<OrderLineQuantity> OrderQuantities() =>
        _lines.OrderBy(l => l.LineNumber).Select(l => new OrderLineQuantity(l.OrderLineId, l.Quantity)).ToList();

    /// <summary>
    /// Approves it under its number with the stock document Inventory posted, each line's stock line and cost, and how the
    /// order split each line, all in line order.
    /// </summary>
    public void Approve(
        DocumentNumber number, Guid stockDocumentId, string stockDocumentNumber,
        IReadOnlyList<(Guid StockLineId, decimal CostAmount)> stock, IReadOnlyList<ReturnSplit> splits, Guid? approverId, DateTime nowUtc)
    {
        var allowed = CanApprove();
        if (allowed.IsFailure)
            throw new InvalidOperationException($"Return {Id} cannot be approved: {allowed.Error}");
        if (stock.Count != _lines.Count || splits.Count != _lines.Count)
            throw new ArgumentException("Every return line has its stock line and split.", nameof(stock));

        var ordered = _lines.OrderBy(l => l.LineNumber).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Settle(stock[i].StockLineId, stock[i].CostAmount, splits[i]);
        Status = SalesReturnStatus.Approved;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        ApprovedByUserId = approverId;
        ApprovedAtUtc = nowUtc;
        StockDocumentId = stockDocumentId;
        StockDocumentNumber = stockDocumentNumber;
        CostAmount = stock.Sum(s => s.CostAmount);
        CreditNetAmount = splits.Sum(s => s.CreditNetAmount);
        CreditTaxAmount = splits.Sum(s => s.CreditTaxAmount);
        CreditTotalAmount = CreditNetAmount + CreditTaxAmount;
        Record(SalesDocumentAction.Approved, approverId, nowUtc, stockDocumentNumber);
    }

    private Result Fill(SalesDelivery delivery, string reason, string? notes, IReadOnlyList<ReturnLineInput> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (lines.Count == 0)
            return SalesReturnDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return SalesReturnDomainErrors.TooManyLines;
        var built = new List<SalesReturnLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var input = lines[i];
            if (input.Condition != ReturnCondition.FitForResale)
                return SalesReturnDomainErrors.ConditionNotSupported(i + 1);
            if (delivery.FindLine(input.DeliveryLineId) is not { } deliveryLine)
                return SalesDeliveryDomainErrors.LineNotFound(i + 1);
            var quantity = input.Unit.CheckQuantity(input.Quantity, i + 1);
            if (quantity.IsFailure)
                return quantity.Error;
            built.Add(SalesReturnLine.Create(i + 1, input, deliveryLine));
        }
        var allowed = delivery.CanReturn(built.Select(l => (l.DeliveryLineId, l.Quantity)).ToList());
        if (allowed.IsFailure)
            return allowed.Error;

        Reason = reason.Trim();
        Notes = Text.Optional(notes);
        _lines.Clear();
        _lines.AddRange(built);
        return Result.Success();
    }

    private void Record(SalesDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(SalesReturnEvent.Create(action, userId, nowUtc, note));
}
