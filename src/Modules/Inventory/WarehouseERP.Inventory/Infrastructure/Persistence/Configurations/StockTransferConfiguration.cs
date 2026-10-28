using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// The transfer only tracks progress; its stock moves through the ledger documents that point back to it.
internal sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> b)
    {
        b.ToTable("StockTransfers", "inventory", t =>
        {
            t.HasCheckConstraint("CK_StockTransfers_Status", "[Status] BETWEEN 1 AND 8");
            t.HasCheckConstraint("CK_StockTransfers_Warehouses",
                "[SourceWarehouseId] <> [DestinationWarehouseId] AND [TransitWarehouseId] <> [SourceWarehouseId] AND [TransitWarehouseId] <> [DestinationWarehouseId]");
            t.HasCheckConstraint("CK_StockTransfers_Dispatched",
                "([Status] IN (1, 2, 8) AND [Number] IS NULL AND [TransitLocationId] IS NULL)"
                + " OR ([Status] IN (3, 4, 5, 6, 7) AND [Number] IS NOT NULL AND [TransitLocationId] IS NOT NULL AND [DispatchedAtUtc] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.ExternalReference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.DispatchedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CompletedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Ignore(x => x.IsOpen);
        b.Ignore(x => x.OutstandingQuantity);

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.TransferId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.TransferId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.SourceWarehouseId, x.Status });
        b.HasIndex(x => new { x.DestinationWarehouseId, x.Status });
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
