using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// Part of the supplier's master data, so versioned the same way (temporal table, history schema).
internal sealed class SupplierAddressConfiguration : IEntityTypeConfiguration<SupplierAddress>
{
    public void Configure(EntityTypeBuilder<SupplierAddress> b)
    {
        b.ToTable("SupplierAddresses", "purchasing", t =>
        {
            t.IsTemporal(h =>
            {
                h.UseHistoryTable("SupplierAddressesHistory", "history");
                h.HasPeriodStart("ValidFrom");
                h.HasPeriodEnd("ValidTo");
            });
            t.HasCheckConstraint("CK_SupplierAddresses_Type", "[AddressType] IN (1, 2, 3)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.AddressType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Line1).HasMaxLength(200);
        b.Property(x => x.Line2).HasMaxLength(200);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.HasIndex(x => x.SupplierId);
    }
}
