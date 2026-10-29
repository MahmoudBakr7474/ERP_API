using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// The same price and VAT checks as Sales' priced lines; what came back never exceeds what was sold, in quantity, net or VAT
// (concurrent returns of one line can't over-return even past the sale's lock). Foreign keys to Catalog and Inventory's
// stock document lines live in Persistence.
internal sealed class PosSaleLineConfiguration : IEntityTypeConfiguration<PosSaleLine>
{
    public void Configure(EntityTypeBuilder<PosSaleLine> b)
    {
        b.ToTable("PosSaleLines", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosSaleLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
            t.HasCheckConstraint("CK_PosSaleLines_Price", "[UnitPrice] > 0 AND [DiscountPercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_PosSaleLines_Tax",
                "([TaxTreatment] = 1 AND [TaxRate] > 0 AND [TaxRate] <= 100) OR ([TaxTreatment] IN (2, 3) AND [TaxRate] = 0)");
            t.HasCheckConstraint("CK_PosSaleLines_Amounts",
                "[GrossAmount] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] = [GrossAmount] - [DiscountAmount] AND [TaxAmount] >= 0"
                + " AND [TotalAmount] = [NetAmount] + [TaxAmount]");
            t.HasCheckConstraint("CK_PosSaleLines_Returned",
                "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity] AND [RefundedNetAmount] >= 0"
                + " AND [RefundedNetAmount] <= [NetAmount] AND [RefundedTaxAmount] >= 0 AND [RefundedTaxAmount] <= [TaxAmount]");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
        b.Property(x => x.Sku).HasMaxLength(50);
        b.Property(x => x.ProductName).HasMaxLength(200);
        b.Property(x => x.UnitCode).HasMaxLength(20);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.UnitPrice).HasPrecision(19, 8);
        b.Property(x => x.DiscountPercent).HasPrecision(9, 4);
        b.Property(x => x.TaxTreatment).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.TaxRate).HasPrecision(9, 4);
        b.Property(x => x.GrossAmount).HasPrecision(19, 4);
        b.Property(x => x.DiscountAmount).HasPrecision(19, 4);
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.NetUnitPrice).HasPrecision(19, 8);
        b.Property(x => x.CostAmount).HasPrecision(19, 4);
        b.Property(x => x.ReturnedQuantity).HasPrecision(18, 4);
        b.Property(x => x.RefundedNetAmount).HasPrecision(19, 4);
        b.Property(x => x.RefundedTaxAmount).HasPrecision(19, 4);
        b.Ignore(x => x.BaseQuantity);
        b.Ignore(x => x.ReturnableQuantity);
        b.HasIndex(x => new { x.SaleId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.ProductId);
        b.HasIndex(x => x.StockLineId).IsUnique().HasFilter("[StockLineId] IS NOT NULL");
    }
}
