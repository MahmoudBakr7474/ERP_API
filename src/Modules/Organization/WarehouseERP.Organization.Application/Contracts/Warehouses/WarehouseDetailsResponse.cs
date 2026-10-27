using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Warehouses;

/// <param name="RowVersion">Base64 row version; send it back with the next update.</param>
public sealed record WarehouseDetailsResponse(
    Guid Id, Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling,
    bool IsActive, string RowVersion, IReadOnlyList<WarehouseLocationResponse> Locations);
