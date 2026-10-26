namespace WarehouseERP.Identity.Application.Contracts.UserAccess;

public sealed record UserAccessResponse(bool IsCompanyWide, IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds);
