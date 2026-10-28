using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <summary>The freezes active stock counts hold.</summary>
public interface IStockFreezes
{
    /// <summary>An active count scope covering one of these places, other than the given count's own, or null.</summary>
    Task<FrozenScope?> FindFrozenAsync(IReadOnlyCollection<StockPlace> places, Guid? exceptCountId, CancellationToken ct);
}
