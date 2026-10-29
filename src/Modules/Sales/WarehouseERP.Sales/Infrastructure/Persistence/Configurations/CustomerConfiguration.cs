using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Master data: a temporal table, like its contacts and addresses, so every earlier value (credit terms and holds
// included) stays in history. The code is unique; a registration is unique within the jurisdiction that issues it
// (filtered indexes, so customers without one don't collide). The name and mobile keys only serve duplicate warnings.
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        TemporalMasterMapping.Map(b, "Customers", "sales");
        // Read back as UTC, like every Sales timestamp, so the API sends them with "Z".
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Customers_Type", "[CustomerType] IN (1, 2)");
            t.HasCheckConstraint("CK_Customers_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
            t.HasCheckConstraint("CK_Customers_CreditLimit", "[CreditLimit] IS NULL OR [CreditLimit] >= 0");
            t.HasCheckConstraint("CK_Customers_CreditHold", "[IsOnCreditHold] = 0 OR [CreditHoldReason] IS NOT NULL");
            // Only a business has a commercial registration.
            t.HasCheckConstraint("CK_Customers_CommercialRegistration", "[CommercialRegistrationNumber] IS NULL OR [CustomerType] = 2");
        });
        b.Property(x => x.Code).HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.CustomerType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.NameAr).HasMaxLength(200);
        b.Property(x => x.NameEn).HasMaxLength(200);
        b.Property(x => x.NameArKey).HasMaxLength(CustomerNames.MaxKeyLength);
        b.Property(x => x.NameEnKey).HasMaxLength(CustomerNames.MaxKeyLength);
        b.Property(x => x.CountryCode).HasMaxLength(2).IsUnicode(false).IsFixedLength();
        b.Property(x => x.TaxRegistrationNumber).HasMaxLength(RegistrationNumbers.MaxLength).IsUnicode(false);
        b.Property(x => x.CommercialRegistrationNumber).HasMaxLength(RegistrationNumbers.MaxLength).IsUnicode(false);
        b.Property(x => x.CommercialRegistry).HasMaxLength(100);
        b.Property(x => x.CommercialRegistryKey).HasMaxLength(CustomerNames.MaxKeyLength);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Mobile).HasMaxLength(30);
        b.Property(x => x.MobileKey).HasMaxLength(CustomerNames.MaxPhoneKeyLength).IsUnicode(false);
        b.Property(x => x.Email).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.CreditLimit).HasPrecision(19, 4);
        b.Property(x => x.CreditHoldReason).HasMaxLength(500);
        b.Property(x => x.CreditChangedAtUtc).IsUtcTimestamp();

        b.HasMany(x => x.Contacts).WithOne().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Addresses).WithOne().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.Code, "UX_Customers_Code").IsUnique();
        b.HasIndex(x => new { x.CountryCode, x.TaxRegistrationNumber }, "UX_Customers_TaxNumber")
            .IsUnique().HasFilter("[TaxRegistrationNumber] IS NOT NULL");
        b.HasIndex(x => new { x.CountryCode, x.CommercialRegistryKey, x.CommercialRegistrationNumber }, "UX_Customers_CommercialRegistration")
            .IsUnique().HasFilter("[CommercialRegistrationNumber] IS NOT NULL");
        b.HasIndex(x => x.NameArKey);
        b.HasIndex(x => x.NameEnKey);
        b.HasIndex(x => x.MobileKey);
    }
}
