namespace WarehouseERP.Organization.Presentation.Contracts.Branches;

public sealed record UpdateBranchRequest(string NameAr, string? NameEn, string RowVersion);
