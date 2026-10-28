using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence.Configurations;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> b)
    {
        TemporalMasterMapping.Map(b, "Cities", "organization");
        b.Property(x => x.NameAr).HasMaxLength(GeographyNames.MaxLength).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(GeographyNames.MaxLength);
        b.HasOne<Governorate>().WithMany().HasForeignKey(x => x.GovernorateId).OnDelete(DeleteBehavior.Restrict);
        // Also the index of the governorate's list.
        b.HasIndex(x => new { x.GovernorateId, x.NameAr }).IsUnique().HasDatabaseName("UX_Cities_GovernorateId_NameAr");
    }
}
