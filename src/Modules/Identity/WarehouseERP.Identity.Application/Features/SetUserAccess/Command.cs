namespace WarehouseERP.Identity.Application.Features.SetUserAccess;

/// <param name="IsCompanyWide">Every warehouse; branch and warehouse grants are then ignored and dropped.</param>
/// <param name="BranchIds">Each branch covers all of its warehouses, including ones created later.</param>
public sealed record SetUserAccessCommand(Guid UserId, bool IsCompanyWide, IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds);
