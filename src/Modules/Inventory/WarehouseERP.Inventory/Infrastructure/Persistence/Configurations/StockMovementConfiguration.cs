using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// The quantity ledger: insert-only, ordered by its identity. No cost columns (InventoryCostEntries hold value).
internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("StockMovements", "inventory", t => t.HasCheckConstraint("CK_StockMovements_Quantity", "[Quantity] <> 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.StockStatus).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<StockAllocation>().WithMany().HasForeignKey(x => x.AllocationId).OnDelete(DeleteBehavior.NoAction);
        // The stock card: one product in one warehouse, in posting order.
        b.HasIndex(x => new { x.ProductId, x.WarehouseId, x.Id }).IncludeProperties(x => new { x.LocationId, x.Quantity });
        b.HasIndex(x => x.DocumentId);
    }
}
