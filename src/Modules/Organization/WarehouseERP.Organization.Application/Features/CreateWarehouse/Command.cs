using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.CreateWarehouse;

public sealed record CreateWarehouseCommand(
    Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling);
