namespace WarehouseERP.Organization.Application.Contracts.Reports;

/// <summary>
/// Counts of the organization the caller may see (a scoped user: their warehouses and the branches holding them).
/// Storage locations are those of physical warehouses; transit locations are opened per dispatched transfer.
/// </summary>
/// <param name="SellingWarehouses">Active warehouses that allow selling.</param>
/// <param name="OpenTransitLocations">Active locations of transit warehouses other than their default one: the transfers on
/// their way (each dispatched transfer opens its own).</param>
/// <param name="DataIssues">Rows of the master-data quality report.</param>
public sealed record OrganizationSummaryResponse(
    int Branches, int ActiveBranches, int Warehouses, int ActiveWarehouses, int PhysicalWarehouses, int TransitWarehouses,
    int SellingWarehouses, int StorageLocations, int ActiveStorageLocations, int OpenTransitLocations, int DataIssues);
