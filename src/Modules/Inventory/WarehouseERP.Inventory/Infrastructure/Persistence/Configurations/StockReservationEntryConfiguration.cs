using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// Insert-only history of each reservation; a consumption names the delivery's stock document.
internal sealed class StockReservationEntryConfiguration : IEntityTypeConfiguration<StockReservationEntry>
{
    public void Configure(EntityTypeBuilder<StockReservationEntry> b)
    {
        b.ToTable("StockReservationEntries", "inventory", t =>
        {
            t.HasCheckConstraint("CK_StockReservationEntries_Kind", "[Kind] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_StockReservationEntries_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_StockReservationEntries_Document",
                "([Kind] = 2 AND [StockDocumentId] IS NOT NULL) OR ([Kind] <> 2 AND [StockDocumentId] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Kind).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.AtUtc).IsUtcTimestamp();

        b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.StockDocumentId).OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(x => x.ReservationId);
        b.HasIndex(x => x.StockDocumentId);
    }
}
