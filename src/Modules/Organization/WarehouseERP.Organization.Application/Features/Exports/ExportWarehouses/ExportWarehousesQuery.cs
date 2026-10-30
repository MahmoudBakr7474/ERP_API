using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportWarehouses;

/// <summary>The warehouse directory with the warehouse list's filters and order.</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportWarehousesQuery(
    Guid? BranchId, string? Search, bool? IsActive, WarehouseType? Type, bool? AllowsSelling, OrganizationSort? Sort,
    bool Descending, int MaxRows);
