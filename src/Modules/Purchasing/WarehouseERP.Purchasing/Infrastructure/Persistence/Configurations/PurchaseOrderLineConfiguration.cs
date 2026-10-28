using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// The database guards what the order checks: nothing received beyond what was ordered (zero over-receipt tolerance),
// nothing returned beyond what was received, and nothing invoiced beyond what was received and not returned before
// invoicing. Only recoverable tax (1) is stored. Foreign keys to Catalog live in Persistence.
internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        b.ToTable("PurchaseOrderLines", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_PurchaseOrderLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
            t.HasCheckConstraint("CK_PurchaseOrderLines_Price",
                "[UnitPrice] >= 0 AND [DiscountPercent] BETWEEN 0 AND 100 AND [TaxRate] BETWEEN 0 AND 100 AND [TaxTreatment] = 1");
            t.HasCheckConstraint("CK_PurchaseOrderLines_Received", "[ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [Quantity]");
            t.HasCheckConstraint("CK_PurchaseOrderLines_Returned",
                "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [ReceivedQuantity]"
                + " AND [ReturnedInvoicedQuantity] >= 0 AND [ReturnedInvoicedQuantity] <= [ReturnedQuantity]");
            t.HasCheckConstraint("CK_PurchaseOrderLines_Invoiced",
                "[InvoicedQuantity] >= 0 AND [InvoicedQuantity] + [ReturnedQuantity] - [ReturnedInvoicedQuantity] <= [ReceivedQuantity]");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
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
        b.Property(x => x.ReceivedQuantity).HasPrecision(18, 4);
        b.Property(x => x.ReturnedQuantity).HasPrecision(18, 4);
        b.Property(x => x.ReturnedInvoicedQuantity).HasPrecision(18, 4);
        b.Property(x => x.InvoicedQuantity).HasPrecision(18, 4);
        b.Property(x => x.InvoicedNetAmount).HasPrecision(19, 4);
        b.Property(x => x.InvoicedTaxAmount).HasPrecision(19, 4);

        b.HasOne<PurchaseRequestLine>().WithMany().HasForeignKey(x => x.PurchaseRequestLineId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.OrderId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.ProductId);
        b.HasIndex(x => x.PurchaseRequestLineId);
    }
}
