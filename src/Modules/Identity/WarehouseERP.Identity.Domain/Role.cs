using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Domain;

/// <summary>
/// A named set of permissions. Roles say *what* a user may do; <see cref="UserAccess"/> says *where*.
/// The system role <see cref="AdministratorName"/> (seeded by Migration02_Identity) holds every permission by
/// definition (nothing stored), so a permission added later is never missing from it; system roles can't be changed
/// or deleted.
/// </summary>
public sealed class Role
{
    public const string AdministratorName = "Administrator";

    private readonly HashSet<string> _permissions;

    private Role(Guid id, string name, string? description, bool isSystem, IEnumerable<string> permissions)
    {
        Id = id;
        Name = name;
        Description = description;
        IsSystem = isSystem;
        _permissions = new HashSet<string>(permissions, StringComparer.Ordinal);
    }

    /// <summary>Empty until the role is stored.</summary>
    public Guid Id { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; }
    public IReadOnlySet<string> Permissions => _permissions;
    public bool GrantsAllPermissions => IsSystem && Name == AdministratorName;

    /// <summary>The stored version (concurrency stamp) this role was read at; null for a new role.</summary>
    public string? Version { get; private init; }

    /// <param name="knownPermissions">The permission catalog; any other name is refused.</param>
    public static Result<Role> Create(string name, string? description, IEnumerable<string> permissions, IReadOnlySet<string> knownPermissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var requested = permissions.ToList();
        if (Unknown(requested, knownPermissions) is { Count: > 0 } unknown)
            return RoleDomainErrors.UnknownPermissions(unknown);
        return new Role(Guid.Empty, name.Trim(), Text.Optional(description), isSystem: false, requested);
    }

    /// <summary>Rebuilds a stored role.</summary>
    public static Role Restore(Guid id, string name, string? description, bool isSystem, IEnumerable<string> permissions, string? version = null) =>
        new(id, name, description, isSystem, permissions) { Version = version };

    /// <summary>Whether the caller edited the role at the version it is stored at (optimistic concurrency).</summary>
    public bool IsAtVersion(string expectedVersion) => string.Equals(Version, expectedVersion, StringComparison.Ordinal);

    public Result Update(string name, string? description, IEnumerable<string> permissions, IReadOnlySet<string> knownPermissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (IsSystem)
            return RoleDomainErrors.SystemRoleLocked;
        var requested = permissions.ToList();
        if (Unknown(requested, knownPermissions) is { Count: > 0 } unknown)
            return RoleDomainErrors.UnknownPermissions(unknown);

        Name = name.Trim();
        Description = Text.Optional(description);
        _permissions.Clear();
        _permissions.UnionWith(requested);
        return Result.Success();
    }

    public Result EnsureDeletable() => IsSystem ? RoleDomainErrors.SystemRoleLocked : Result.Success();

    private static List<string> Unknown(IEnumerable<string> requested, IReadOnlySet<string> known) =>
        requested.Where(p => !known.Contains(p)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
}
