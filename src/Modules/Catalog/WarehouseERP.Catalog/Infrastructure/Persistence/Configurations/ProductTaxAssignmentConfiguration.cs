using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence.Configurations;

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
