using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;

public sealed record CreateWarehouseCommand(
    Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling);
