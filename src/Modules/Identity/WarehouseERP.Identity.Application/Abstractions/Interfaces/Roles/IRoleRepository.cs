using WarehouseERP.Identity.Application.Contracts.Roles;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;

/// <summary>Roles and their permissions.</summary>
public interface IRoleRepository
{
    Task<Role?> GetAsync(Guid roleId, CancellationToken ct);

    Task<Guid?> FindAdministratorIdAsync(CancellationToken ct);

    Task<bool> NameExistsAsync(string name, Guid? excludingRoleId, CancellationToken ct);

    /// <summary>The roles among these ids that exist.</summary>
    Task<IReadOnlyList<Role>> GetManyAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken ct);

    /// <summary>Tracks a new role and returns its id.</summary>
    Task<Guid> AddAsync(Role role, CancellationToken ct);

    Task UpdateAsync(Role role, CancellationToken ct);

    /// <summary>
    /// Gives the role a new version in the coming save, checked against the version read now, so concurrent saves
    /// that rely on who holds the role conflict instead of both committing.
    /// </summary>
    Task TouchAsync(Guid roleId, CancellationToken ct);

    Task DeleteAsync(Guid roleId, CancellationToken ct);

    Task<IReadOnlyList<RoleSummaryResponse>> ListAsync(CancellationToken ct);
}
