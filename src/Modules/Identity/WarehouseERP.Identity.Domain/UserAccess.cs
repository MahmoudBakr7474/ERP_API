using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Identity.Domain;

/// <summary>
/// Where a user may act: company-wide, or a set of branches (each covering all of its warehouses, including
/// ones created later) plus individual warehouses. The single source of this information.
/// </summary>
public sealed class UserAccess : IAuditable
{
    private readonly List<UserBranchGrant> _branches = [];
    private readonly List<UserWarehouseGrant> _warehouses = [];

    private UserAccess() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsCompanyWide { get; private set; }

    /// <summary>Grows with every change, so the row (and who changed it) is stamped and versioned.</summary>
    public int Revision { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public IReadOnlyCollection<UserBranchGrant> Branches => _branches;
    public IReadOnlyCollection<UserWarehouseGrant> Warehouses => _warehouses;

    public static UserAccess Create(Guid userId, bool companyWide, IEnumerable<Guid> branchIds, IEnumerable<Guid> warehouseIds)
    {
        var access = new UserAccess { UserId = userId };
        access.Apply(companyWide, branchIds, warehouseIds);
        return access;
    }

    /// <summary>
    /// Replaces the scope. Company-wide access makes branch and warehouse grants redundant, so they are dropped.
    /// Only the difference is applied, keeping the temporal history readable.
    /// </summary>
    public void Set(bool companyWide, IEnumerable<Guid> branchIds, IEnumerable<Guid> warehouseIds)
    {
        Apply(companyWide, branchIds, warehouseIds);
        Revision++;
    }

    private void Apply(bool companyWide, IEnumerable<Guid> branchIds, IEnumerable<Guid> warehouseIds)
    {
        IsCompanyWide = companyWide;
        var branches = companyWide ? [] : branchIds.Where(id => id != Guid.Empty).ToHashSet();
        var warehouses = companyWide ? [] : warehouseIds.Where(id => id != Guid.Empty).ToHashSet();

        _branches.RemoveAll(g => !branches.Contains(g.BranchId));
        foreach (var id in branches.Except(_branches.Select(g => g.BranchId)))
            _branches.Add(new UserBranchGrant(id));
        _warehouses.RemoveAll(g => !warehouses.Contains(g.WarehouseId));
        foreach (var id in warehouses.Except(_warehouses.Select(g => g.WarehouseId)))
            _warehouses.Add(new UserWarehouseGrant(id));
    }
}

public sealed class UserBranchGrant
{
    private UserBranchGrant() { } // EF Core

    internal UserBranchGrant(Guid branchId) => BranchId = branchId;

    public Guid UserAccessId { get; private set; }
    public Guid BranchId { get; private set; }
}

public sealed class UserWarehouseGrant
{
    private UserWarehouseGrant() { } // EF Core

    internal UserWarehouseGrant(Guid warehouseId) => WarehouseId = warehouseId;

    public Guid UserAccessId { get; private set; }
    public Guid WarehouseId { get; private set; }
}
