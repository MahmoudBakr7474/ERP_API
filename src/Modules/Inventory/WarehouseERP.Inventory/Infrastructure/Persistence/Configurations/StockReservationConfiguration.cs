using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// Operational data: no temporal table; its history is StockReservationEntries. The database guards what the reservation
// checks: it never gives back or delivers more than it reserved. One reservation per sales order line. Foreign keys to
// Organization and Catalog live in Persistence.
internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> b)
    {
        b.ToTable("StockReservations", "inventory", t => t.HasCheckConstraint("CK_StockReservations_Quantities",
            "[ReservedQuantity] > 0 AND [ConsumedQuantity] >= 0 AND [ReleasedQuantity] >= 0"
            + " AND [ConsumedQuantity] + [ReleasedQuantity] <= [ReservedQuantity]"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SourceNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.ReservedQuantity).HasPrecision(18, 4);
        b.Property(x => x.ConsumedQuantity).HasPrecision(18, 4);
        b.Property(x => x.ReleasedQuantity).HasPrecision(18, 4);
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Ignore(x => x.RemainingQuantity);

        b.HasMany(x => x.Entries).WithOne().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.SourceLineId).IsUnique();
        b.HasIndex(x => x.SourceDocumentId);
        b.HasIndex(x => new { x.WarehouseId, x.ProductId });
    }
}
