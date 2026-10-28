using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// A physical count of one physical warehouse, whole or some of its locations. Starting it freezes its scope (persisted
/// in StockCountScope, no lock held while people count) and snapshots the expected quantities; counted quantities are
/// entered in base units; approval (its creator may give it too) posts the differences as a count adjustment and releases
/// the freeze.
/// </summary>
public sealed class StockCount : IAuditable
{
    public const int MaxLocations = 200;

    private readonly List<StockCountScope> _scopes = [];
    private readonly List<StockCountLine> _lines = [];
    private readonly List<StockCountEvent> _events = [];

    private StockCount() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Number of its reference (CT-000123); gaps are expected.</summary>
    public int DraftNumber { get; private set; }

    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public StockCountStatus Status { get; private set; }

    /// <summary>True when the count covers every location of the warehouse, including ones added while it runs.</summary>
    public bool IsWholeWarehouse { get; private set; }

    public string? Notes { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public Guid? StartedByUserId { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<StockCountScope> Scopes => _scopes;
    public IReadOnlyList<StockCountLine> Lines => _lines;
    public IReadOnlyList<StockCountEvent> Events => _events;

    /// <summary>The lines whose counted quantity differs from the expected one.</summary>
    public IEnumerable<StockCountLine> Differences => _lines.Where(l => l.Variance != 0);

    /// <summary>A draft count of the whole warehouse (no locations given) or of these locations.</summary>
    public static Result<StockCount> Create(
        Guid warehouseId, Guid branchId, IReadOnlyCollection<Guid> locationIds, string? notes, Guid? userId, DateTime nowUtc)
    {
        var locations = locationIds.Distinct().ToList();
        if (locations.Count > MaxLocations)
            return StockCountDomainErrors.TooManyLocations;

        var count = new StockCount
        {
            WarehouseId = warehouseId,
            BranchId = branchId,
            Status = StockCountStatus.Draft,
            IsWholeWarehouse = locations.Count == 0,
            Notes = Text.Optional(notes),
        };
        if (count.IsWholeWarehouse)
            count._scopes.Add(StockCountScope.For(warehouseId, null));
        else
            count._scopes.AddRange(locations.Select(l => StockCountScope.For(warehouseId, l)));
        count.Record(StockCountAction.Created, userId, nowUtc, note: null);
        return count;
    }

    public bool Covers(Guid locationId) => IsWholeWarehouse || _scopes.Any(s => s.LocationId == locationId);

    /// <summary>Freezes the scope and keeps the expected quantities; the caller holds the warehouse's exclusive lock.</summary>
    public Result Start(IReadOnlyList<CountSnapshotLine> snapshot, Guid? userId, DateTime nowUtc)
    {
        if (Status != StockCountStatus.Draft)
            return StockCountDomainErrors.NotDraft;

        foreach (var scope in _scopes)
            scope.Activate();
        _lines.AddRange(snapshot.Select(s => StockCountLine.Expected(WarehouseId, s.ProductId, s.LocationId, s.Quantity)));
        Status = StockCountStatus.Counting;
        StartedAtUtc = nowUtc;
        StartedByUserId = userId;
        Record(StockCountAction.Started, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Records counted quantities; a product found where nothing was expected gets a line expecting zero.</summary>
    public Result EnterCounts(IReadOnlyList<CountEntry> entries)
    {
        if (Status != StockCountStatus.Counting)
            return StockCountDomainErrors.NotCounting;
        foreach (var entry in entries)
        {
            if (!Covers(entry.LocationId))
                return StockCountDomainErrors.OutOfScope;
            ArgumentOutOfRangeException.ThrowIfNegative(entry.CountedQuantity);
            if (!entry.BaseUnitAllowsFraction && !Quantities.IsWhole(entry.CountedQuantity))
                return StockCountDomainErrors.QuantityMustBeWhole;
        }

        foreach (var entry in entries)
        {
            var line = _lines.SingleOrDefault(l => l.ProductId == entry.ProductId && l.LocationId == entry.LocationId);
            if (line is null)
            {
                line = StockCountLine.Expected(WarehouseId, entry.ProductId, entry.LocationId, 0m);
                _lines.Add(line);
            }
            line.Count(entry.CountedQuantity, entry.UnitCost);
        }
        return Result.Success();
    }

    public Result Submit(Guid? userId, DateTime nowUtc)
    {
        if (Status != StockCountStatus.Counting)
            return StockCountDomainErrors.NotCounting;
        if (_lines.Any(l => l.CountedQuantity is null))
            return StockCountDomainErrors.NotFullyCounted;

        Status = StockCountStatus.Submitted;
        SubmittedAtUtc = nowUtc;
        Record(StockCountAction.Submitted, userId, nowUtc, note: null);
        return Result.Success();
    }

    /// <summary>Sends a submitted count back for counting, saying why; the scope stays frozen.</summary>
    public Result Reject(string reason, Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != StockCountStatus.Submitted)
            return StockCountDomainErrors.NotSubmitted;

        Status = StockCountStatus.Counting;
        Record(StockCountAction.Rejected, userId, nowUtc, reason.Trim());
        return Result.Success();
    }

    /// <summary>Abandons the count and releases its freeze; nothing posts.</summary>
    public Result Cancel(Guid? userId, DateTime nowUtc)
    {
        if (Status is not (StockCountStatus.Draft or StockCountStatus.Counting or StockCountStatus.Submitted))
            return StockCountDomainErrors.NotOpen;

        foreach (var scope in _scopes)
            scope.Release();
        Status = StockCountStatus.Cancelled;
        Record(StockCountAction.Cancelled, userId, nowUtc, note: null);
        return Result.Success();
    }

    public Result CanApprove() =>
        Status == StockCountStatus.Submitted ? Result.Success() : StockCountDomainErrors.NotSubmitted;

    /// <summary>Completes an approved count once the caller posted its differences, and releases the freeze.</summary>
    public void Complete(string? adjustmentNumber, Guid? userId, DateTime nowUtc)
    {
        var allowed = CanApprove();
        if (allowed.IsFailure)
            throw new InvalidOperationException($"Count {Id} cannot be approved: {allowed.Error}");

        foreach (var scope in _scopes)
            scope.Release();
        ApprovedByUserId = userId;
        Status = StockCountStatus.Completed;
        CompletedAtUtc = nowUtc;
        Record(StockCountAction.Approved, userId, nowUtc, note: null);
        Record(StockCountAction.Completed, userId, nowUtc, adjustmentNumber);
    }

    private void Record(StockCountAction action, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(StockCountEvent.Create(action, userId, nowUtc, note));
}
