using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

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
