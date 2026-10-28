using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// Valuation history: insert-only, one entry per posted line.
internal sealed class InventoryCostEntryConfiguration : IEntityTypeConfiguration<InventoryCostEntry>
{
    public void Configure(EntityTypeBuilder<InventoryCostEntry> b)
    {
        b.ToTable("InventoryCostEntries", "inventory", t =>
        {
            t.HasCheckConstraint("CK_InventoryCostEntries_QuantityDelta", "[QuantityDelta] <> 0");
            t.HasCheckConstraint("CK_InventoryCostEntries_After", "[QuantityAfter] >= 0 AND [ValueAfter] >= 0 AND [AverageCostAfter] >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.QuantityDelta).HasPrecision(18, 4);
        b.Property(x => x.ValueDelta).HasPrecision(19, 4);
        b.Property(x => x.UnitCost).HasPrecision(19, 8);
        b.Property(x => x.QuantityAfter).HasPrecision(18, 4);
        b.Property(x => x.ValueAfter).HasPrecision(19, 4);
        b.Property(x => x.AverageCostAfter).HasPrecision(19, 8);
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => x.LineId).IsUnique();
        b.HasIndex(x => new { x.ProductId, x.Id });
        b.HasIndex(x => x.DocumentId);
    }
}
