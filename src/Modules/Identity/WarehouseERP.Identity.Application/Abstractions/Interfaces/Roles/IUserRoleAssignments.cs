using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;

/// <summary>Which users hold which roles.</summary>
public interface IUserRoleAssignments
{
    Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, CancellationToken ct);

    /// <summary>Replaces the user's roles.</summary>
    Task<Result> SetRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct);

    Task<int> CountActiveUsersInRoleAsync(Guid roleId, Guid? excludingUserId, CancellationToken ct);

    Task<bool> AnyUserInRoleAsync(Guid roleId, CancellationToken ct);
}
