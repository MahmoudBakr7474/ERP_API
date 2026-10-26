using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// The identity schema. ASP.NET Core Identity's own mapping (keys, lengths, unique UserNameIndex/RoleNameIndex)
// comes from IdentityDbContext; these configurations move its tables into the schema, add the ERP columns and
// Identity's own tables. Who could do what, and when, stays answerable: roles, permissions, role assignments and
// scope keep temporal history. Users don't (history would keep every old password hash), nor do sessions.
// The foreign keys from grants to organization.Branches/Warehouses are configured in ErpDbContext (ADR 0001).
internal static class IdentityTables
{
    public const string Schema = "identity";

    public static void ToTemporalTable<TEntity>(this EntityTypeBuilder<TEntity> builder, string table)
        where TEntity : class =>
        builder.ToTable(table, Schema, tb => tb.IsTemporal(t =>
        {
            t.UseHistoryTable(table + "History", "history");
            t.HasPeriodStart("ValidFrom");
            t.HasPeriodEnd("ValidTo");
        }));

    public static void MapAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(nameof(IAuditable.CreatedAtUtc)).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(nameof(IAuditable.UpdatedAtUtc)).HasColumnType("datetime2(3)");
    }
}

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users", IdentityTables.Schema);
        builder.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.MapAudit();
    }
}

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTemporalTable("Roles");
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsSystem).IsRequired();
        builder.MapAudit();

        // Holds every permission by definition (nothing stored), so a permission added later is never missing.
        builder.HasData(new ApplicationRole
        {
            Id = ApplicationRole.AdministratorId,
            Name = Role.AdministratorName,
            NormalizedName = Role.AdministratorName.ToUpperInvariant(),
            ConcurrencyStamp = "5d2f9b8e-1c4a-4e7f-8b3d-6a9e0c1f2d47",
            Description = "Every permission, including user and role management.",
            IsSystem = true,
            CreatedAtUtc = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) => builder.ToTemporalTable("UserRoles");
}

internal sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder) => builder.ToTemporalTable("RoleClaims");
}

// Unused by the ERP today; kept in the schema because ASP.NET Core Identity maps them (external logins later).
internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("UserClaims", IdentityTables.Schema);
}

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("UserLogins", IdentityTables.Schema);
}

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("UserTokens", IdentityTables.Schema);
}

internal sealed class UserAccessConfiguration : IEntityTypeConfiguration<UserAccess>
{
    public void Configure(EntityTypeBuilder<UserAccess> builder)
    {
        builder.ToTemporalTable("UserAccess");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserAccess>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        // Every change bumps Revision, so two administrators editing one user's scope can't overwrite each other.
        builder.Property(x => x.Revision).IsConcurrencyToken();
        builder.MapAudit();

        builder.HasMany(x => x.Branches).WithOne().HasForeignKey(x => x.UserAccessId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Warehouses).WithOne().HasForeignKey(x => x.UserAccessId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Branches).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Warehouses).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UserBranchGrantConfiguration : IEntityTypeConfiguration<UserBranchGrant>
{
    public void Configure(EntityTypeBuilder<UserBranchGrant> builder)
    {
        builder.ToTemporalTable("UserBranchAccess");
        builder.HasKey(x => new { x.UserAccessId, x.BranchId });
    }
}

internal sealed class UserWarehouseGrantConfiguration : IEntityTypeConfiguration<UserWarehouseGrant>
{
    public void Configure(EntityTypeBuilder<UserWarehouseGrant> builder)
    {
        builder.ToTemporalTable("UserWarehouseAccess");
        builder.HasKey(x => new { x.UserAccessId, x.WarehouseId });
    }
}

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    private const int HashLength = 32; // SHA-256

    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions", IdentityTables.Schema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.DeviceId).HasMaxLength(100);
        builder.Property(x => x.TokenHash).HasMaxLength(HashLength).IsFixedLength().IsRequired();
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(HashLength).IsFixedLength();
        builder.Property(x => x.PreviousRefreshTokenHash).HasMaxLength(HashLength).IsFixedLength();
        foreach (var time in new[]
                 {
                     nameof(UserSession.TokenExpiresAtUtc), nameof(UserSession.RefreshExpiresAtUtc), nameof(UserSession.CreatedAtUtc),
                     nameof(UserSession.LastSeenAtUtc), nameof(UserSession.ExpiresAtUtc), nameof(UserSession.RevokedAtUtc),
                 })
            builder.Property(time).HasColumnType("datetime2(3)");

        // Every authenticated request looks its session up by token hash; a refresh by the current or previous
        // refresh-token hash (the previous one to detect reuse).
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.RefreshTokenHash).IsUnique().HasFilter("[RefreshTokenHash] IS NOT NULL");
        builder.HasIndex(x => x.PreviousRefreshTokenHash).HasFilter("[PreviousRefreshTokenHash] IS NOT NULL");
    }
}
