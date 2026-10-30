using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Reports;

/// <summary>A storage location with the warehouse and branch it belongs to (both fixed once it is created).</summary>
public sealed record LocationDirectoryResponse(
    Guid Id, string Code, string NameAr, string? NameEn, bool IsDefault, bool IsActive,
    Guid WarehouseId, string WarehouseCode, string WarehouseNameAr, string? WarehouseNameEn, WarehouseType WarehouseType,
    bool WarehouseIsActive, Guid BranchId, string BranchCode, string BranchNameAr, string? BranchNameEn, DateTime CreatedAtUtc);
