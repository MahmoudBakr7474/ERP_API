using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence.Configurations;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> b)
    {
        TemporalMasterMapping.Map(b, "Countries", "organization");
        b.Property(x => x.Code).HasMaxLength(Country.CodeLength).IsFixedLength().IsUnicode(false).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(GeographyNames.MaxLength).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(GeographyNames.MaxLength);
        // The ISO code is the country's natural key: other modules' rows (suppliers, their addresses) point at it.
        b.HasAlternateKey(x => x.Code).HasName("AK_Countries_Code");
        b.HasIndex(x => x.NameAr).IsUnique().HasDatabaseName("UX_Countries_NameAr");
    }
}
