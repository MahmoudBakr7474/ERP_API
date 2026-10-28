namespace WarehouseERP.Organization.Contracts;

/// <summary>
/// Locations Inventory opens in a transit warehouse, one per dispatched transfer, and retires once the transfer is done.
/// Both join the caller's unit of work: the change is saved by the caller's save point, inside its transaction.
/// </summary>
public interface ITransitLocations
{
    /// <summary>A new location in the transit warehouse, or null when it is not an active transit warehouse.</summary>
    Task<Guid?> OpenAsync(Guid transitWarehouseId, string code, string nameAr, CancellationToken ct);

    /// <summary>Retires a transfer's location once it holds nothing more.</summary>
    Task RetireAsync(Guid transitWarehouseId, Guid locationId, CancellationToken ct);
}
