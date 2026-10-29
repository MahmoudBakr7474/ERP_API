using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// The columns every priced line keeps (quotation, order and invoice lines), with the same checks: a positive price, a
// discount of 0 to 100, and VAT that matches its treatment (standard: a rate above 0; zero-rated and exempt: 0).
internal static class PricedLineMapping
{
    public static void Map<TLine>(EntityTypeBuilder<TLine> b, string table, Action<TableBuilder<TLine>>? checks = null)
        where TLine : PricedLine
    {
        b.ToTable(table, "sales", t =>
        {
            t.HasCheckConstraint($"CK_{table}_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
            t.HasCheckConstraint($"CK_{table}_Price",
                "[UnitPrice] > 0 AND [PriceSource] IN (1, 2) AND [DiscountPercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint($"CK_{table}_Tax",
                "([TaxTreatment] = 1 AND [TaxRate] > 0 AND [TaxRate] <= 100) OR ([TaxTreatment] IN (2, 3) AND [TaxRate] = 0)");
            checks?.Invoke(t);
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.UnitPrice).HasPrecision(19, 8);
        b.Property(x => x.PriceSource).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.ListUnitPrice).HasPrecision(19, 8);
        b.Property(x => x.DiscountPercent).HasPrecision(9, 4);
        b.Property(x => x.TaxTreatment).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.TaxRate).HasPrecision(9, 4);
        b.Property(x => x.GrossAmount).HasPrecision(19, 4);
        b.Property(x => x.DiscountAmount).HasPrecision(19, 4);
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.NetUnitPrice).HasPrecision(19, 8);
        b.Ignore(x => x.BaseQuantity);
        b.Ignore(x => x.Price);
        b.HasIndex(x => x.ProductId);
    }
}
