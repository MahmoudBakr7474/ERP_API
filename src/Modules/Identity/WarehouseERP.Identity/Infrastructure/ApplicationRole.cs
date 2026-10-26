using Microsoft.AspNetCore.Identity;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Identity.Infrastructure;

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
