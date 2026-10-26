namespace WarehouseERP.Identity.Presentation.Contracts.Users;

public sealed record SetAccessRequest(bool IsCompanyWide, IReadOnlyList<Guid>? BranchIds, IReadOnlyList<Guid>? WarehouseIds);
