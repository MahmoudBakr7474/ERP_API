namespace WarehouseERP.Organization.Application.Features.Branches.SetBranchActive;

public sealed record SetBranchActiveCommand(Guid BranchId, bool IsActive);
