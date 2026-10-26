using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence.Configurations;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        TemporalMasterMapping.Map(b, "Companies", "organization");
        b.Property(x => x.Code).HasMaxLength(20).IsRequired().IsUnicode(false);
        b.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(200);
        b.Property(x => x.BaseCurrencyCode).HasColumnType("char(3)").IsRequired();
        b.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired().IsUnicode(false);
        b.Property(x => x.TaxRegistrationNumber).HasMaxLength(50).IsUnicode(false);
        b.HasIndex(x => x.Code).IsUnique();
    }
}
