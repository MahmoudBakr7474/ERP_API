using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Part of the customer's master data, so versioned the same way (temporal table, history schema). The foreign key to
// Organization's cities lives in Persistence.
internal sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> b)
    {
        b.ToTable("CustomerAddresses", "sales", t =>
        {
            t.IsTemporal(h =>
            {
                h.UseHistoryTable("CustomerAddressesHistory", "history");
                h.HasPeriodStart("ValidFrom");
                h.HasPeriodEnd("ValidTo");
            });
            t.HasCheckConstraint("CK_CustomerAddresses_Type", "[AddressType] IN (1, 2, 3)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.AddressType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Line1).HasMaxLength(200);
        b.Property(x => x.Line2).HasMaxLength(200);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.HasIndex(x => x.CustomerId);
    }
}
