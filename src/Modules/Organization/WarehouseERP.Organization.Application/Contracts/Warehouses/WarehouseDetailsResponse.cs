using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Warehouses;

public sealed record WarehouseDetailsResponse(
    Guid Id, Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling,
    bool IsActive, IReadOnlyList<WarehouseLocationResponse> Locations);
