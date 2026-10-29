using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Part of the customer's master data, so versioned the same way (temporal table, history schema).
internal sealed class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> b)
    {
        b.ToTable("CustomerContacts", "sales", t => t.IsTemporal(h =>
        {
            h.UseHistoryTable("CustomerContactsHistory", "history");
            h.HasPeriodStart("ValidFrom");
            h.HasPeriodEnd("ValidTo");
        }));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.JobTitle).HasMaxLength(100);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(100);
        b.HasIndex(x => x.CustomerId);
    }
}
