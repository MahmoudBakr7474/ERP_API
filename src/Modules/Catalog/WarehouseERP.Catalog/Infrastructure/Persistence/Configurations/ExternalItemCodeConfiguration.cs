using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence.Configurations;

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
