using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        TemporalMasterMapping.Map(b, "Branches", "organization");
        b.Property(x => x.Code).HasMaxLength(30).IsRequired().IsUnicode(false);
        b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(150);
        b.HasOne(x => x.Company).WithMany(x => x.Branches)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}
