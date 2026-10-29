using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Products' tax classifications are master data: a temporal table keeps every earlier one. One row per product, active or
// removed; a rate only on standard-rated rows. The foreign key to Catalog lives in Persistence.
internal sealed class ProductTaxConfiguration : IEntityTypeConfiguration<ProductTax>
{
    public void Configure(EntityTypeBuilder<ProductTax> b)
    {
        TemporalMasterMapping.Map(b, "ProductTaxes", "sales");
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.ToTable(t => t.HasCheckConstraint("CK_ProductTaxes_Rate",
            "([TaxTreatment] = 1 AND ([TaxRate] IS NULL OR ([TaxRate] > 0 AND [TaxRate] <= 100)))"
            + " OR ([TaxTreatment] IN (2, 3) AND [TaxRate] IS NULL)"));
        b.Property(x => x.TaxTreatment).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.TaxRate).HasPrecision(9, 4);
        b.HasIndex(x => x.ProductId, "UX_ProductTaxes_Product").IsUnique();
    }
}
