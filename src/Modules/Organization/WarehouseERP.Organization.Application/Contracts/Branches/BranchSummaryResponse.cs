namespace WarehouseERP.Organization.Application.Contracts.Branches;

public sealed record BranchSummaryResponse(Guid Id, string Code, string NameAr, string? NameEn, bool IsActive);
