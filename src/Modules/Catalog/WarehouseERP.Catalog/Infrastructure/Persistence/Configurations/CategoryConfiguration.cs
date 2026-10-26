using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence.Configurations;

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
