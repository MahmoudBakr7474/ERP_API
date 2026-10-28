using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockDocumentLineConfiguration : IEntityTypeConfiguration<StockDocumentLine>
{
    public void Configure(EntityTypeBuilder<StockDocumentLine> b)
    {
        b.ToTable("StockDocumentLines", "inventory", t =>
        {
            t.HasCheckConstraint("CK_StockDocumentLines_Quantities", "[Quantity] > 0 AND [BaseQuantity] > 0 AND [ConversionFactor] > 0");
            t.HasCheckConstraint("CK_StockDocumentLines_Direction", "[Direction] IN (1, 2, 3)");
            t.HasCheckConstraint("CK_StockDocumentLines_UnitCost", "[UnitCost] IS NULL OR ([UnitCost] >= 0 AND [Direction] = 1)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.BaseQuantity).HasPrecision(18, 4);
        b.Property(x => x.UnitCost).HasPrecision(19, 8);
        b.Property(x => x.Direction).HasConversion<byte>().HasColumnType("tinyint");

        b.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.ReversesLineId).OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(x => new { x.DocumentId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.ProductId);
    }
}
