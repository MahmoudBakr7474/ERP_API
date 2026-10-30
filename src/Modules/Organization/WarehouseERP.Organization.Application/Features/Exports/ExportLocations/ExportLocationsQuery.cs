using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportLocations;

/// <summary>The location directory with its filters and order.</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportLocationsQuery(
    Guid? BranchId, Guid? WarehouseId, WarehouseType? WarehouseType, string? Search, bool? IsActive, OrganizationSort? Sort,
    bool Descending, int MaxRows);
