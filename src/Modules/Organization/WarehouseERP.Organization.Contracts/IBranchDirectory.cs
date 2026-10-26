namespace WarehouseERP.Organization.Contracts;

/// <summary>Read-only view of branches for other modules.</summary>
public interface IBranchDirectory
{
    /// <summary>The branches among these ids that exist; unknown ids are left out.</summary>
    Task<IReadOnlyList<BranchInfo>> FindManyAsync(IReadOnlyCollection<Guid> branchIds, CancellationToken ct);
}
