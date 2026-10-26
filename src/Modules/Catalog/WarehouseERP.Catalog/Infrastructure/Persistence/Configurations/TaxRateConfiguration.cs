using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence.Configurations;

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
