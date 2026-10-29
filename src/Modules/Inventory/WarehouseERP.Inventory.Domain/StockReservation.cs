using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// Sellable stock held in one physical warehouse for one sales order line, in base units. It is reserved once, when
/// the order is confirmed, and then only shrinks: deliveries consume it and reductions, closing or cancelling release
/// it, never beyond what remains. The warehouse's ProductWarehouseStock.ReservedQuantity is the sum of what its
/// reservations still hold; both change in the same transaction. Every change is an insert-only entry.
/// </summary>
public sealed class StockReservation : IAuditable
{
    private readonly List<StockReservationEntry> _entries = [];

    private StockReservation() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>The sales order.</summary>
    public Guid SourceDocumentId { get; private set; }

    /// <summary>The sales order line; unique.</summary>
    public Guid SourceLineId { get; private set; }

    /// <summary>The order's number when it reserved.</summary>
    public string SourceNumber { get; private set; } = null!;

    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal ConsumedQuantity { get; private set; }
    public decimal ReleasedQuantity { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<StockReservationEntry> Entries => _entries;

    /// <summary>What the reservation still holds.</summary>
    public decimal RemainingQuantity => ReservedQuantity - ConsumedQuantity - ReleasedQuantity;

    public static StockReservation Reserve(
        Guid sourceDocumentId, Guid sourceLineId, string sourceNumber, Guid productId, Guid warehouseId, decimal baseQuantity,
        Guid? userId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseQuantity);
        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            SourceDocumentId = sourceDocumentId,
            SourceLineId = sourceLineId,
            SourceNumber = sourceNumber.Trim(),
            ProductId = productId,
            WarehouseId = warehouseId,
            ReservedQuantity = baseQuantity,
        };
        reservation._entries.Add(StockReservationEntry.Create(ReservationEntryKind.Reserved, baseQuantity, null, null, userId, nowUtc));
        return reservation;
    }

    /// <summary>A delivery took this much out of the reservation; never more than it holds.</summary>
    public Result Consume(decimal baseQuantity, Guid stockDocumentId, Guid? userId, DateTime nowUtc, int line)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseQuantity);
        if (baseQuantity > RemainingQuantity)
            return StockReservationDomainErrors.Exceeded(line, RemainingQuantity);
        ConsumedQuantity += baseQuantity;
        _entries.Add(StockReservationEntry.Create(ReservationEntryKind.Consumed, baseQuantity, stockDocumentId, null, userId, nowUtc));
        return Result.Success();
    }

    /// <summary>Gives back this much (null: all that remains); returns what was released, possibly nothing.</summary>
    public Result<decimal> Release(decimal? baseQuantity, string reason, Guid? userId, DateTime nowUtc, int line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var quantity = baseQuantity ?? RemainingQuantity;
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(baseQuantity), baseQuantity, "A release gives back a positive quantity.");
        if (quantity > RemainingQuantity)
            return StockReservationDomainErrors.Exceeded(line, RemainingQuantity);
        if (quantity == 0)
            return 0m;
        ReleasedQuantity += quantity;
        _entries.Add(StockReservationEntry.Create(ReservationEntryKind.Released, quantity, null, Text.Optional(reason), userId, nowUtc));
        return quantity;
    }
}
