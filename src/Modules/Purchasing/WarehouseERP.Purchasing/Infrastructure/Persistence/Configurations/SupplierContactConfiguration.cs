using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// Part of the supplier's master data, so versioned the same way (temporal table, history schema).
internal sealed class SupplierContactConfiguration : IEntityTypeConfiguration<SupplierContact>
{
    public void Configure(EntityTypeBuilder<SupplierContact> b)
    {
        b.ToTable("SupplierContacts", "purchasing", t => t.IsTemporal(h =>
        {
            h.UseHistoryTable("SupplierContactsHistory", "history");
            h.HasPeriodStart("ValidFrom");
            h.HasPeriodEnd("ValidTo");
        }));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.JobTitle).HasMaxLength(100);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(100);
        b.HasIndex(x => x.SupplierId);
    }
}
