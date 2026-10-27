namespace WarehouseERP.Organization.Application.Features.Branches.UpdateBranch;

/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateBranchCommand(Guid BranchId, string NameAr, string? NameEn, string RowVersion);
