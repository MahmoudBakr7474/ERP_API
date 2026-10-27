namespace WarehouseERP.Organization.Application.Contracts.Branches;

/// <param name="RowVersion">Base64 row version; send it back with the next update.</param>
public sealed record BranchDetailsResponse(
    Guid Id, Guid CompanyId, string Code, string NameAr, string? NameEn, bool IsActive, string RowVersion);
