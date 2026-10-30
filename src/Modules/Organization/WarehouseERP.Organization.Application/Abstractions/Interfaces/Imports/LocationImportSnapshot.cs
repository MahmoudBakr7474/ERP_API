using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Imports;

/// <summary>
/// The organization as the import sees it, tracked for changes: the branches the file names, their warehouses, and the
/// locations of their physical warehouses.
/// </summary>
public sealed record LocationImportSnapshot(
    IReadOnlyList<Branch> Branches, IReadOnlyList<Warehouse> Warehouses, IReadOnlyList<WarehouseLocation> Locations);
