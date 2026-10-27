namespace WarehouseERP.Identity.Application.Features.Users.SetUserAccess;

/// <param name="IsCompanyWide">Every warehouse; branch and warehouse grants are then ignored and dropped.</param>
/// <param name="BranchIds">Each branch covers all of its warehouses, including ones created later.</param>
/// <param name="Revision">The access revision the caller read (GET user → access.revision); a newer one means someone
/// else changed the scope meanwhile, and the request is refused instead of overwriting their change.</param>
public sealed record SetUserAccessCommand(
    Guid UserId, bool IsCompanyWide, IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds, int Revision);
