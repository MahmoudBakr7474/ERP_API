using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockCountEventConfiguration : IEntityTypeConfiguration<StockCountEvent>
{
    public void Configure(EntityTypeBuilder<StockCountEvent> b)
    {
        b.ToTable("StockCountEvents", "inventory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Action).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.AtUtc).IsUtcTimestamp();
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => x.CountId);
    }
}
