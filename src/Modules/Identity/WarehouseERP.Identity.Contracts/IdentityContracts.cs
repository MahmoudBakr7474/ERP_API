namespace WarehouseERP.Identity.Contracts;

/// <summary>Permissions Identity endpoints require.</summary>
public static class IdentityPermissions
{
    /// <summary>List and read users.</summary>
    public const string UsersRead = "identity.users.read";

    /// <summary>Create users, edit profiles, activate/deactivate, unlock, reset passwords, assign roles.</summary>
    public const string UsersManage = "identity.users.manage";

    /// <summary>Set where a user may act (company-wide, branches, warehouses).</summary>
    public const string AccessManage = "identity.access.manage";

    /// <summary>List roles and the permission catalog.</summary>
    public const string RolesRead = "identity.roles.read";

    /// <summary>Create, edit and delete roles.</summary>
    public const string RolesManage = "identity.roles.manage";

    /// <summary>See and revoke other users' sessions.</summary>
    public const string SessionsManage = "identity.sessions.manage";

    /// <summary>Every Identity permission.</summary>
    public static IReadOnlyList<string> All { get; } =
        [UsersRead, UsersManage, AccessManage, RolesRead, RolesManage, SessionsManage];
}

/// <summary>Who a user is, for other modules (e.g. to show "created by").</summary>
public interface IUserDirectory
{
    /// <summary>Display names of the users among these ids that exist.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);
}
