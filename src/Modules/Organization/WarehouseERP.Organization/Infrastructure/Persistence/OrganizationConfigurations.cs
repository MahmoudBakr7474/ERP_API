using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

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

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        TemporalMasterMapping.Map(b, "Warehouses", "organization");
        b.Property(x => x.Code).HasMaxLength(30).IsRequired().IsUnicode(false);
        b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(150);
        b.Property(x => x.WarehouseType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        b.Property(x => x.AllowsSelling).IsRequired();
        b.HasOne(x => x.Branch).WithMany(x => x.Warehouses)
            .HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Warehouses_Type", "[WarehouseType] IN (1, 2)");
            t.HasCheckConstraint("CK_Warehouses_TransitNoSales", "[WarehouseType] <> 2 OR [AllowsSelling] = 0");
        });
    }
}

internal sealed class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseLocation> b)
    {
        TemporalMasterMapping.Map(b, "WarehouseLocations", "organization");
        b.Property(x => x.Code).HasMaxLength(50).IsUnicode(false).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(150);
        b.Property(x => x.IsDefault).IsRequired();
        b.HasOne(x => x.Warehouse).WithMany(x => x.Locations)
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
        b.HasIndex(x => x.WarehouseId, "UX_WarehouseLocations_OneDefault")
            .IsUnique().HasFilter("[IsDefault] = 1");
        // Used by composite foreign keys from Inventory module later.
        b.HasAlternateKey(x => new { x.WarehouseId, x.Id });
    }
}
