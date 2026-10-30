using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Warehouses;

/// <param name="LocationCount">All the warehouse's locations; <paramref name="ActiveLocationCount"/> of them active.</param>
/// <param name="DefaultLocationCode">Where stock goes when no location is named; null when the warehouse has none.</param>
/// <param name="DefaultLocationIsActive">Whether that default location is active; null without one.</param>
public sealed record WarehouseSummaryResponse(
    Guid Id, Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling, bool IsActive,
    string BranchCode, string BranchNameAr, string? BranchNameEn, bool BranchIsActive, int LocationCount, int ActiveLocationCount,
    string? DefaultLocationCode, bool? DefaultLocationIsActive);
