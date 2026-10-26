using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        TemporalMasterMapping.Map(b, "Products", "catalog");
        b.Property(x => x.SKU).HasMaxLength(80).IsUnicode(false).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(250).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(250);
        b.Property(x => x.ProductType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        b.Property(x => x.TrackingMode).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.BaseUnit).WithMany().HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => x.SKU).IsUnique();
        b.HasIndex(x => x.CategoryId);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Products_Type", "[ProductType] IN (1, 2, 3)");
            t.HasCheckConstraint("CK_Products_Tracking", "[TrackingMode] IN (0, 1, 2)");
            t.HasCheckConstraint("CK_Products_NonStockNoTracking", "[ProductType] = 1 OR [TrackingMode] = 0");
        });
    }
}
