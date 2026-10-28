using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// Master data: a temporal table, like its contacts and addresses. The code is unique; a registration is unique within
// the jurisdiction that issues it (filtered indexes, so suppliers without one don't collide). The name keys only serve
// duplicate warnings.
internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        TemporalMasterMapping.Map(b, "Suppliers", "purchasing");
        b.ToTable(t => t.HasCheckConstraint("CK_Suppliers_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365"));
        b.Property(x => x.Code).HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.NameAr).HasMaxLength(200);
        b.Property(x => x.NameEn).HasMaxLength(200);
        b.Property(x => x.NameArKey).HasMaxLength(SupplierNames.MaxKeyLength);
        b.Property(x => x.NameEnKey).HasMaxLength(SupplierNames.MaxKeyLength);
        b.Property(x => x.CountryCode).HasMaxLength(2).IsUnicode(false).IsFixedLength();
        b.Property(x => x.TaxRegistrationNumber).HasMaxLength(RegistrationNumbers.MaxLength).IsUnicode(false);
        b.Property(x => x.CommercialRegistrationNumber).HasMaxLength(RegistrationNumbers.MaxLength).IsUnicode(false);
        b.Property(x => x.CommercialRegistry).HasMaxLength(100);
        b.Property(x => x.CommercialRegistryKey).HasMaxLength(SupplierNames.MaxKeyLength);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasMany(x => x.Contacts).WithOne().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Addresses).WithOne().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.Code, "UX_Suppliers_Code").IsUnique();
        b.HasIndex(x => new { x.CountryCode, x.TaxRegistrationNumber }, "UX_Suppliers_TaxNumber")
            .IsUnique().HasFilter("[TaxRegistrationNumber] IS NOT NULL");
        b.HasIndex(x => new { x.CountryCode, x.CommercialRegistryKey, x.CommercialRegistrationNumber }, "UX_Suppliers_CommercialRegistration")
            .IsUnique().HasFilter("[CommercialRegistrationNumber] IS NOT NULL");
        b.HasIndex(x => x.NameArKey);
        b.HasIndex(x => x.NameEnKey);
    }
}
