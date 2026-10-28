using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> b)
    {
        b.ToTable("StockTransferLines", "inventory", t => t.HasCheckConstraint("CK_StockTransferLines_Quantities",
            "[Quantity] > 0 AND [BaseQuantity] > 0 AND [ConversionFactor] > 0 AND [ReceivedQuantity] >= 0 AND [LostQuantity] >= 0"
            + " AND [ReceivedQuantity] + [LostQuantity] <= [BaseQuantity]"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.BaseQuantity).HasPrecision(18, 4);
        b.Property(x => x.ReceivedQuantity).HasPrecision(18, 4);
        b.Property(x => x.LostQuantity).HasPrecision(18, 4);
        b.Ignore(x => x.OutstandingQuantity);
        b.HasIndex(x => new { x.TransferId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.ProductId);
    }
}
