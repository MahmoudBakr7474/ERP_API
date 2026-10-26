using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> b)
    {
        TemporalMasterMapping.Map(b, "ProductUnits", "catalog");
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8).IsRequired();
        b.HasOne(x => x.Product).WithMany(x => x.Units)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.ProductId, x.UnitId }).IsUnique();
        // Additional principal key supports matching ProductId in ExternalItemCodes.
        b.HasAlternateKey(x => new { x.Id, x.ProductId });
        b.ToTable(t => t.HasCheckConstraint("CK_ProductUnits_ConversionFactor", "[ConversionFactor] > 0"));
    }
}
