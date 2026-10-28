using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// No check that reserved stays within on hand: an authorized loss may over-reserve (INVENTORY_DECISIONS.md).
internal sealed class ProductWarehouseStockConfiguration : IEntityTypeConfiguration<ProductWarehouseStock>
{
    public void Configure(EntityTypeBuilder<ProductWarehouseStock> b)
    {
        b.ToTable("ProductWarehouseStock", "inventory", t =>
        {
            t.HasCheckConstraint("CK_ProductWarehouseStock_OnHandSellable", "[OnHandSellable] >= 0");
            t.HasCheckConstraint("CK_ProductWarehouseStock_ReservedQuantity", "[ReservedQuantity] >= 0");
        });
        b.HasKey(x => new { x.ProductId, x.WarehouseId });
        b.Property(x => x.OnHandSellable).HasPrecision(18, 4);
        b.Property(x => x.ReservedQuantity).HasPrecision(18, 4);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.WarehouseId);
    }
}
