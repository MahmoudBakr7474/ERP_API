using Microsoft.AspNetCore.Identity;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Identity.Infrastructure;

// ASP.NET Core Identity's user and role rows (identity.Users, identity.Roles) with what the ERP adds. Public only
// because ErpDbContext names them as IdentityDbContext's type arguments; no other module uses them.

/// <summary>An account: credentials and lockout are ASP.NET Core Identity's, the rest is the ERP's.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>, IAuditable
{
    public string DisplayName { get; set; } = "";

    /// <summary>An inactive user can't sign in, and their sessions were revoked when they were deactivated.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

/// <summary>A role; its permissions are role claims of type <c>permission</c>.</summary>
public sealed class ApplicationRole : IdentityRole<Guid>, IAuditable
{
    /// <summary>The system Administrator role, seeded by Migration02_Identity.</summary>
    public static readonly Guid AdministratorId = new("3b0c6d0e-5f7a-4c1e-9a52-7d4f1e2b8c61");

    public string? Description { get; set; }

    /// <summary>System roles can't be edited or deleted.</summary>
    public bool IsSystem { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
