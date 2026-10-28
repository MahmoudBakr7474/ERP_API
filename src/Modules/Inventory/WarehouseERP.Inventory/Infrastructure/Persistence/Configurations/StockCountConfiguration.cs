using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> b)
    {
        b.ToTable("StockCounts", "inventory", t => t.HasCheckConstraint("CK_StockCounts_Status", "[Status] BETWEEN 1 AND 5"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.StartedAtUtc).IsUtcTimestamp();
        b.Property(x => x.SubmittedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CompletedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Ignore(x => x.Differences);

        b.HasMany(x => x.Scopes).WithOne().HasForeignKey(x => x.CountId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.CountId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.CountId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
