using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> b)
    {
        TemporalMasterMapping.Map(b, "ProductBarcodes", "catalog");
        b.Property(x => x.Barcode).HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(x => x.BarcodeType).HasConversion<byte>().HasColumnType("tinyint");
        b.HasOne(x => x.ProductUnit).WithMany(x => x.Barcodes)
            .HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => x.Barcode).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_ProductBarcodes_Type", "[BarcodeType] IN (1, 2, 3, 4)"));
    }
}
