using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// The price list is master data: a temporal table keeps every earlier price. One row per product unit, active or removed.
// Foreign keys to Catalog live in Persistence.
internal sealed class SalesPriceConfiguration : IEntityTypeConfiguration<SalesPrice>
{
    public void Configure(EntityTypeBuilder<SalesPrice> b)
    {
        TemporalMasterMapping.Map(b, "SalesPrices", "sales");
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.ToTable(t => t.HasCheckConstraint("CK_SalesPrices_UnitPrice", "[UnitPrice] > 0"));
        b.Property(x => x.UnitPrice).HasPrecision(19, 8);
        b.HasIndex(x => x.ProductUnitId, "UX_SalesPrices_ProductUnit").IsUnique();
        b.HasIndex(x => x.ProductId);
    }
}
