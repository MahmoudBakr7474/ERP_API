namespace WarehouseERP.Organization.Application.Contracts.Branches;

/// <param name="WarehouseCount">The branch's warehouses the caller may see; <paramref name="ActiveWarehouseCount"/> of them active.</param>
public sealed record BranchSummaryResponse(
    Guid Id, string Code, string NameAr, string? NameEn, bool IsActive, int WarehouseCount, int ActiveWarehouseCount);
