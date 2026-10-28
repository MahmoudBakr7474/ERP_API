using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// Each side is a warehouse and one of its locations (a composite key to WarehouseLocations, in Persistence).
internal sealed class StockAllocationConfiguration : IEntityTypeConfiguration<StockAllocation>
{
    public void Configure(EntityTypeBuilder<StockAllocation> b)
    {
        b.ToTable("StockAllocations", "inventory", t =>
        {
            t.HasCheckConstraint("CK_StockAllocations_Quantity", "[BaseQuantity] > 0");
            t.HasCheckConstraint("CK_StockAllocations_From",
                "([FromWarehouseId] IS NULL AND [FromLocationId] IS NULL) OR ([FromWarehouseId] IS NOT NULL AND [FromLocationId] IS NOT NULL)");
            t.HasCheckConstraint("CK_StockAllocations_To",
                "([ToWarehouseId] IS NULL AND [ToLocationId] IS NULL) OR ([ToWarehouseId] IS NOT NULL AND [ToLocationId] IS NOT NULL)");
            t.HasCheckConstraint("CK_StockAllocations_OneSide", "[FromLocationId] IS NOT NULL OR [ToLocationId] IS NOT NULL");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.BaseQuantity).HasPrecision(18, 4);
        b.HasIndex(x => x.LineId);
        b.HasIndex(x => new { x.FromWarehouseId, x.FromLocationId });
        b.HasIndex(x => new { x.ToWarehouseId, x.ToLocationId });
    }
}
