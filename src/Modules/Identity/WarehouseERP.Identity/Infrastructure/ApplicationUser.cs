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
