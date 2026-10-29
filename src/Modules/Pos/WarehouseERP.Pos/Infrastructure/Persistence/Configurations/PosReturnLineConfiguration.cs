using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// V1 takes back only goods fit for resale (condition 1). Foreign keys to Catalog and Inventory's stock lines live in
// Persistence.
internal sealed class PosReturnLineConfiguration : IEntityTypeConfiguration<PosReturnLine>
{
    public void Configure(EntityTypeBuilder<PosReturnLine> b)
    {
        b.ToTable("PosReturnLines", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosReturnLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
            t.HasCheckConstraint("CK_PosReturnLines_Condition", "[Condition] = 1");
            t.HasCheckConstraint("CK_PosReturnLines_Amounts",
                "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount]");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
        b.Property(x => x.Sku).HasMaxLength(50);
        b.Property(x => x.ProductName).HasMaxLength(200);
        b.Property(x => x.UnitCode).HasMaxLength(20);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.Condition).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.UnitPrice).HasPrecision(19, 8);
        b.Property(x => x.DiscountPercent).HasPrecision(9, 4);
        b.Property(x => x.TaxTreatment).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.TaxRate).HasPrecision(9, 4);
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.CostAmount).HasPrecision(19, 4);
        b.Ignore(x => x.BaseQuantity);
        b.HasOne<PosSaleLine>().WithMany().HasForeignKey(x => x.SaleLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ReturnId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.SaleLineId);
        b.HasIndex(x => x.StockLineId).IsUnique().HasFilter("[StockLineId] IS NOT NULL");
    }
}
