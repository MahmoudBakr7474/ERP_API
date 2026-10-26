using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        TemporalMasterMapping.Map(b, "Warehouses", "organization");
        b.Property(x => x.Code).HasMaxLength(30).IsRequired().IsUnicode(false);
        b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(150);
        b.Property(x => x.WarehouseType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        b.Property(x => x.AllowsSelling).IsRequired();
        b.HasOne(x => x.Branch).WithMany(x => x.Warehouses)
            .HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Warehouses_Type", "[WarehouseType] IN (1, 2)");
            t.HasCheckConstraint("CK_Warehouses_TransitNoSales", "[WarehouseType] <> 2 OR [AllowsSelling] = 0");
        });
    }
}
