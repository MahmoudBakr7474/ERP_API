using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseLocation> b)
    {
        TemporalMasterMapping.Map(b, "WarehouseLocations", "organization");
        b.Property(x => x.Code).HasMaxLength(50).IsUnicode(false).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(150);
        b.Property(x => x.IsDefault).IsRequired();
        b.HasOne(x => x.Warehouse).WithMany(x => x.Locations)
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
        b.HasIndex(x => x.WarehouseId, "UX_WarehouseLocations_OneDefault")
            .IsUnique().HasFilter("[IsDefault] = 1");
        // Used by composite foreign keys from Inventory module later.
        b.HasAlternateKey(x => new { x.WarehouseId, x.Id });
    }
}
