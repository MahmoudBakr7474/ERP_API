using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence.Configurations;

internal sealed class GovernorateConfiguration : IEntityTypeConfiguration<Governorate>
{
    public void Configure(EntityTypeBuilder<Governorate> b)
    {
        TemporalMasterMapping.Map(b, "Governorates", "organization");
        b.Property(x => x.NameAr).HasMaxLength(GeographyNames.MaxLength).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(GeographyNames.MaxLength);
        b.HasOne<Country>().WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Restrict);
        // Also the index of the country's list.
        b.HasIndex(x => new { x.CountryId, x.NameAr }).IsUnique().HasDatabaseName("UX_Governorates_CountryId_NameAr");
    }
}
