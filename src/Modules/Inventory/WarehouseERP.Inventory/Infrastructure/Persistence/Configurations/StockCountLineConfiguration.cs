using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> b)
    {
        b.ToTable("StockCountLines", "inventory", t => t.HasCheckConstraint("CK_StockCountLines_Quantities",
            "[ExpectedQuantity] >= 0 AND ([CountedQuantity] IS NULL OR [CountedQuantity] >= 0) AND ([UnitCost] IS NULL OR [UnitCost] >= 0)"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ExpectedQuantity).HasPrecision(18, 4);
        b.Property(x => x.CountedQuantity).HasPrecision(18, 4);
        b.Property(x => x.UnitCost).HasPrecision(19, 8);
        b.Ignore(x => x.Variance);
        b.HasIndex(x => new { x.CountId, x.ProductId, x.LocationId }).IsUnique();
    }
}
