using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// Goods leaving the order's warehouse for the customer. As a draft it is the picking list: order lines, quantities in
/// the order line's unit and the locations to pick from (one order line may be picked from several), nothing touched in
/// stock. Posting numbers it and, in the same transaction, Inventory takes each line out of its location and the order
/// line's reservation, at the moving average, and the order records what was delivered. Posted deliveries never change;
/// customer returns count against their lines.
/// </summary>
public sealed class SalesDelivery : IAuditable
{
    public const int MaxLines = 500;

    private readonly List<SalesDeliveryLine> _lines = [];
    private readonly List<SalesDeliveryEvent> _events = [];

    private SalesDelivery() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of the draft reference (DDN-000123), from an identity column; gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    public string? Number { get; private set; }
    public short? FiscalYear { get; private set; }
    public int? SequenceNumber { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public SalesDeliveryStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? PostedByUserId { get; private set; }

    /// <summary>The SalesDelivery stock document Inventory posted (it names this delivery back).</summary>
    public Guid? StockDocumentId { get; private set; }

    public string? StockDocumentNumber { get; private set; }

    /// <summary>What the delivered goods cost at the moving average, in the base currency.</summary>
    public decimal CostAmount { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<SalesDeliveryLine> Lines => _lines;
    public IReadOnlyList<SalesDeliveryEvent> Events => _events;

    public static Result<SalesDelivery> CreateDraft(
        SalesOrder order, string? notes, IReadOnlyList<DeliveryLineInput> lines, Guid? userId, DateTime nowUtc)
    {
        var delivery = new SalesDelivery
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            WarehouseId = order.WarehouseId,
            BranchId = order.BranchId,
            Status = SalesDeliveryStatus.Draft,
        };
        var filled = delivery.Fill(order, notes, lines);
        if (filled.IsFailure)
            return filled.Error;
        delivery.Record(SalesDocumentAction.Created, userId, nowUtc, note: null);
        return delivery;
    }

    /// <summary>Replaces a draft's lines and notes.</summary>
    public Result ReplaceDraft(SalesOrder order, string? notes, IReadOnlyList<DeliveryLineInput> lines) =>
        Status != SalesDeliveryStatus.Draft ? SalesDeliveryDomainErrors.NotDraft : Fill(order, notes, lines);

    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesDeliveryStatus.Draft)
            return SalesDeliveryDomainErrors.NotDraft;
        Status = SalesDeliveryStatus.Cancelled;
        Record(SalesDocumentAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>What it delivers per order line, for the order.</summary>
    public IReadOnlyList<OrderLineQuantity> OrderQuantities() =>
        _lines.OrderBy(l => l.LineNumber).Select(l => new OrderLineQuantity(l.OrderLineId, l.Quantity)).ToList();

    /// <summary>
    /// Marks it posted under its number with the stock document Inventory posted and each line's stock line and cost, in
    /// line order.
    /// </summary>
    public void MarkPosted(
        DocumentNumber number, Guid stockDocumentId, string stockDocumentNumber, IReadOnlyList<(Guid StockLineId, decimal CostAmount)> lines,
        Guid? userId, DateTime nowUtc)
    {
        if (Status != SalesDeliveryStatus.Draft)
            throw new InvalidOperationException($"Delivery {Id} is already {Status}.");
        if (lines.Count != _lines.Count)
            throw new ArgumentException("Every delivery line has its stock line.", nameof(lines));

        var ordered = _lines.OrderBy(l => l.LineNumber).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].LinkStock(lines[i].StockLineId, lines[i].CostAmount);
        Status = SalesDeliveryStatus.Posted;
        Number = number.Number;
        FiscalYear = number.FiscalYear;
        SequenceNumber = number.SequenceNumber;
        PostedAtUtc = nowUtc;
        PostedByUserId = userId;
        StockDocumentId = stockDocumentId;
        StockDocumentNumber = stockDocumentNumber;
        CostAmount = lines.Sum(l => l.CostAmount);
        Record(SalesDocumentAction.Posted, userId, nowUtc, stockDocumentNumber);
    }

    /// <summary>Checks returned quantities against what each of its lines still has unreturned; nothing changes.</summary>
    public Result CanReturn(IReadOnlyList<(Guid DeliveryLineId, decimal Quantity)> returned)
    {
        if (Status != SalesDeliveryStatus.Posted)
            return SalesDeliveryDomainErrors.NotPosted;
        for (var i = 0; i < returned.Count; i++)
            if (FindLine(returned[i].DeliveryLineId) is null)
                return SalesDeliveryDomainErrors.LineNotFound(i + 1);
        foreach (var group in returned.Select((r, i) => (Line: r, Number: i + 1)).GroupBy(x => x.Line.DeliveryLineId))
        {
            var line = FindLine(group.Key)!;
            if (group.Sum(x => x.Line.Quantity) > line.ReturnableQuantity)
                return SalesDeliveryDomainErrors.ReturnExceedsDelivery(group.First().Number, line.ReturnableQuantity);
        }
        return Result.Success();
    }

    /// <summary>Records goods that came back from its lines, never more than each delivered and has not had back yet.</summary>
    public Result RecordReturn(IReadOnlyList<(Guid DeliveryLineId, decimal Quantity)> returned)
    {
        var allowed = CanReturn(returned);
        if (allowed.IsFailure)
            return allowed;
        foreach (var (lineId, quantity) in returned)
            FindLine(lineId)!.AddReturned(quantity);
        return Result.Success();
    }

    public SalesDeliveryLine? FindLine(Guid lineId) => _lines.SingleOrDefault(l => l.Id == lineId);

    private Result Fill(SalesOrder order, string? notes, IReadOnlyList<DeliveryLineInput> lines)
    {
        if (lines.Count == 0)
            return SalesDeliveryDomainErrors.NoLines;
        if (lines.Count > MaxLines)
            return SalesDeliveryDomainErrors.TooManyLines;
        if (lines.GroupBy(l => (l.OrderLineId, l.LocationId)).Any(g => g.Count() > 1))
            return SalesDeliveryDomainErrors.DuplicatePick;
        var built = new List<SalesDeliveryLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var input = lines[i];
            if (order.FindLine(input.OrderLineId) is not { } orderLine)
                return SalesOrderDomainErrors.LineNotFound(i + 1);
            var quantity = input.Unit.CheckQuantity(input.Quantity, i + 1);
            if (quantity.IsFailure)
                return quantity.Error;
            built.Add(SalesDeliveryLine.Create(i + 1, input, orderLine));
        }
        var allowed = order.CanDeliver(built.Select(l => new OrderLineQuantity(l.OrderLineId, l.Quantity)).ToList());
        if (allowed.IsFailure)
            return allowed.Error;

        Notes = Text.Optional(notes);
        _lines.Clear();
        _lines.AddRange(built);
        return Result.Success();
    }

    private void Record(SalesDocumentAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(SalesDeliveryEvent.Create(action, userId, nowUtc, note));
}
