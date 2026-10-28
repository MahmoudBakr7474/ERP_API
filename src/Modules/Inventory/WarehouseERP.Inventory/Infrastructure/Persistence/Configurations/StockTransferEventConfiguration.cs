using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockTransferEventConfiguration : IEntityTypeConfiguration<StockTransferEvent>
{
    public void Configure(EntityTypeBuilder<StockTransferEvent> b)
    {
        b.ToTable("StockTransferEvents", "inventory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Action).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.AtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => x.TransferId);
    }
}
