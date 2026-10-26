using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

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
