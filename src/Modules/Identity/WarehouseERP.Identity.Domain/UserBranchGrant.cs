namespace WarehouseERP.Identity.Domain;

public sealed class UserBranchGrant
{
    private UserBranchGrant() { } // EF Core

    internal UserBranchGrant(Guid branchId) => BranchId = branchId;

    public Guid UserAccessId { get; private set; }
    public Guid BranchId { get; private set; }
}
