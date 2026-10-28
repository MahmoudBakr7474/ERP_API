using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> b)
    {
        b.ToTable("StockBalances", "inventory", t => t.HasCheckConstraint("CK_StockBalances_Quantity", "[Quantity] >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.StockStatus).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        // Unfiltered on purpose (INVENTORY_DECISIONS.md): SQL Server's unique index then treats a null lot as one value,
        // so there is exactly one row per product, location and status while lots don't exist.
        b.HasIndex(x => new { x.ProductId, x.WarehouseId, x.LocationId, x.LotId, x.StockStatus }, "UX_StockBalances_Key")
            .IsUnique().HasFilter(null);
        b.HasIndex(x => new { x.WarehouseId, x.LocationId });
    }
}
