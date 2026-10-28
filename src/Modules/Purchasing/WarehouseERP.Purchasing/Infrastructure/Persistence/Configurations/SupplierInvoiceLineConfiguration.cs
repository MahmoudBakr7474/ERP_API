using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class SupplierInvoiceLineConfiguration : IEntityTypeConfiguration<SupplierInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceLine> b)
    {
        b.ToTable("SupplierInvoiceLines", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_SupplierInvoiceLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_SupplierInvoiceLines_Price",
                "[UnitPrice] >= 0 AND [DiscountPercent] BETWEEN 0 AND 100 AND [TaxRate] BETWEEN 0 AND 100 AND [TaxTreatment] = 1");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.UnitPrice).HasPrecision(19, 8);
        b.Property(x => x.DiscountPercent).HasPrecision(9, 4);
        b.Property(x => x.TaxRate).HasPrecision(9, 4);
        b.Property(x => x.TaxTreatment).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.GrossAmount).HasPrecision(19, 4);
        b.Property(x => x.DiscountAmount).HasPrecision(19, 4);
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.NetUnitPrice).HasPrecision(19, 8);
        b.Property(x => x.OrderNetUnitPrice).HasPrecision(19, 8);
        b.Property(x => x.PriceVarianceAmount).HasPrecision(19, 4);
        b.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.InvoiceId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.PurchaseOrderLineId);
    }
}
