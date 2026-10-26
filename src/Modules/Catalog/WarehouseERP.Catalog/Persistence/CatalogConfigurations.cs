using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Persistence;

namespace WarehouseERP.Catalog.Persistence;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        TemporalMasterMapping.Map(b, "Categories", "catalog");
        b.Property(x => x.Code).HasMaxLength(30).IsUnicode(false).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(150);
        b.HasOne(x => x.Parent).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => x.Code).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_Categories_ParentNotSelf",
            "[ParentId] IS NULL OR [ParentId] <> [Id]"));
        // Multi-level cycle detection belongs in catalog application service.
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> b)
    {
        TemporalMasterMapping.Map(b, "Units", "catalog");
        b.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(100);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        TemporalMasterMapping.Map(b, "Products", "catalog");
        b.Property(x => x.SKU).HasMaxLength(80).IsUnicode(false).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(250).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(250);
        b.Property(x => x.ProductType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        b.Property(x => x.TrackingMode).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.BaseUnit).WithMany().HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => x.SKU).IsUnique();
        b.HasIndex(x => x.CategoryId);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Products_Type", "[ProductType] IN (1, 2, 3)");
            t.HasCheckConstraint("CK_Products_Tracking", "[TrackingMode] IN (0, 1, 2)");
            t.HasCheckConstraint("CK_Products_NonStockNoTracking", "[ProductType] = 1 OR [TrackingMode] = 0");
        });
    }
}

internal sealed class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> b)
    {
        TemporalMasterMapping.Map(b, "ProductUnits", "catalog");
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8).IsRequired();
        b.HasOne(x => x.Product).WithMany(x => x.Units)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.ProductId, x.UnitId }).IsUnique();
        // Additional principal key supports matching ProductId in ExternalItemCodes.
        b.HasAlternateKey(x => new { x.Id, x.ProductId });
        b.ToTable(t => t.HasCheckConstraint("CK_ProductUnits_ConversionFactor", "[ConversionFactor] > 0"));
    }
}

internal sealed class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> b)
    {
        TemporalMasterMapping.Map(b, "ProductBarcodes", "catalog");
        b.Property(x => x.Barcode).HasMaxLength(100).IsUnicode(false).IsRequired();
        b.Property(x => x.BarcodeType).HasConversion<byte>().HasColumnType("tinyint");
        b.HasOne(x => x.ProductUnit).WithMany(x => x.Barcodes)
            .HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => x.Barcode).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_ProductBarcodes_Type", "[BarcodeType] IN (1, 2, 3, 4)"));
    }
}

internal sealed class ExternalItemCodeConfiguration : IEntityTypeConfiguration<ExternalItemCode>
{
    public void Configure(EntityTypeBuilder<ExternalItemCode> b)
    {
        TemporalMasterMapping.Map(b, "ExternalItemCodes", "catalog");
        b.Property(x => x.Code).HasMaxLength(150).IsUnicode(false).IsRequired();
        b.Property(x => x.CodeType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.ApprovalStatus).HasConversion<byte>().HasColumnType("tinyint");
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction);
        // Composite FK prevents linking a code to another product's ProductUnit.
        b.HasOne(x => x.ProductUnit).WithMany()
            .HasForeignKey(x => new { x.ProductUnitId, x.ProductId })
            .HasPrincipalKey(x => new { x.Id, x.ProductId })
            .OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.CodeType, x.Code }).IsUnique();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ExternalItemCodes_Type", "[CodeType] IN (1, 2)");
            t.HasCheckConstraint("CK_ExternalItemCodes_Approval", "[ApprovalStatus] IN (1, 2, 3)");
        });
    }
}

internal sealed class TaxCodeConfiguration : IEntityTypeConfiguration<TaxCode>
{
    public void Configure(EntityTypeBuilder<TaxCode> b)
    {
        TemporalMasterMapping.Map(b, "TaxCodes", "catalog");
        b.Property(x => x.Code).HasMaxLength(30).IsUnicode(false).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(150);
        b.Property(x => x.TaxType).HasConversion<byte>().HasColumnType("tinyint");
        b.HasIndex(x => x.Code).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_TaxCodes_Type", "[TaxType] IN (1, 2)"));
    }
}

internal sealed class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> b)
    {
        TemporalMasterMapping.Map(b, "TaxRates", "catalog");
        b.Property(x => x.Rate).HasPrecision(9, 6);
        b.Property(x => x.EffectiveFrom).HasColumnType("date");
        b.Property(x => x.EffectiveTo).HasColumnType("date");
        b.HasOne(x => x.TaxCode).WithMany().HasForeignKey(x => x.TaxCodeId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.TaxCodeId, x.EffectiveFrom }).IsUnique();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TaxRates_Rate", "[Rate] >= 0");
            t.HasCheckConstraint("CK_TaxRates_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
        });
    }
}

internal sealed class ProductTaxAssignmentConfiguration : IEntityTypeConfiguration<ProductTaxAssignment>
{
    public void Configure(EntityTypeBuilder<ProductTaxAssignment> b)
    {
        TemporalMasterMapping.Map(b, "ProductTaxAssignments", "catalog");
        b.Property(x => x.EffectiveFrom).HasColumnType("date");
        b.Property(x => x.EffectiveTo).HasColumnType("date");
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.TaxCode).WithMany().HasForeignKey(x => x.TaxCodeId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.ProductId, x.TaxCodeId, x.EffectiveFrom }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_ProductTaxAssignments_Dates",
            "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]"));
    }
}
