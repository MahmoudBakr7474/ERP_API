using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;

/// <summary>Write side of the sales price list, and the active prices lines are priced from.</summary>
public interface ISalesPriceRepository
{
    /// <summary>The active price of each of these product units that has one, by product unit.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> FindActivePricesAsync(IReadOnlyCollection<Guid> productUnitIds, CancellationToken ct);

    /// <summary>The product unit's price row, active or removed, tracked; null when it never had one.</summary>
    Task<SalesPrice?> GetForUpdateAsync(Guid productUnitId, CancellationToken ct);

    void Add(SalesPrice price);

    /// <summary>The save point; a unit priced twice at once comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
