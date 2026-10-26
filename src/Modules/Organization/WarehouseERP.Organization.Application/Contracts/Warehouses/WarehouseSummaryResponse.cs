using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Warehouses;

public sealed record WarehouseSummaryResponse(
    Guid Id, Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling, bool IsActive);
