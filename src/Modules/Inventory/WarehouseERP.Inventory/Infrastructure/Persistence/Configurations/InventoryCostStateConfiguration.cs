using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class InventoryCostStateConfiguration : IEntityTypeConfiguration<InventoryCostState>
{
    public void Configure(EntityTypeBuilder<InventoryCostState> b)
    {
        b.ToTable("InventoryCostStates", "inventory", t =>
        {
            t.HasCheckConstraint("CK_InventoryCostStates_TotalQuantity", "[TotalQuantity] >= 0");
            t.HasCheckConstraint("CK_InventoryCostStates_TotalValue", "[TotalValue] >= 0");
            t.HasCheckConstraint("CK_InventoryCostStates_AverageUnitCost", "[AverageUnitCost] >= 0");
        });
        b.HasKey(x => x.ProductId);
        b.Property(x => x.ProductId).ValueGeneratedNever();
        b.Property(x => x.TotalQuantity).HasPrecision(18, 4);
        b.Property(x => x.TotalValue).HasPrecision(19, 4);
        b.Property(x => x.AverageUnitCost).HasPrecision(19, 8);
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
