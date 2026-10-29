using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// The database guards what the order checks: nothing delivered or closed beyond what was ordered, nothing returned beyond
// what was delivered, and nothing invoiced beyond what was delivered and not returned before invoicing. Foreign keys to
// Catalog live in Persistence.
internal sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> b)
    {
        PricedLineMapping.Map(b, "SalesOrderLines", t =>
        {
            t.HasCheckConstraint("CK_SalesOrderLines_Delivered",
                "[DeliveredQuantity] >= 0 AND [ClosedQuantity] >= 0 AND [DeliveredQuantity] + [ClosedQuantity] <= [Quantity]");
            t.HasCheckConstraint("CK_SalesOrderLines_Returned",
                "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [DeliveredQuantity]"
                + " AND [ReturnedInvoicedQuantity] >= 0 AND [ReturnedInvoicedQuantity] <= [ReturnedQuantity]");
            t.HasCheckConstraint("CK_SalesOrderLines_Invoiced",
                "[InvoicedQuantity] >= 0 AND [InvoicedQuantity] + [ReturnedQuantity] - [ReturnedInvoicedQuantity] <= [DeliveredQuantity]");
        });
        b.Property(x => x.DeliveredQuantity).HasPrecision(18, 4);
        b.Property(x => x.ClosedQuantity).HasPrecision(18, 4);
        b.Property(x => x.ReturnedQuantity).HasPrecision(18, 4);
        b.Property(x => x.ReturnedInvoicedQuantity).HasPrecision(18, 4);
        b.Property(x => x.InvoicedQuantity).HasPrecision(18, 4);
        b.Property(x => x.InvoicedNetAmount).HasPrecision(19, 4);
        b.Property(x => x.InvoicedTaxAmount).HasPrecision(19, 4);
        b.Ignore(x => x.RemainingQuantity);
        b.Ignore(x => x.InvoiceableQuantity);
        b.Ignore(x => x.ReturnableQuantity);
        b.HasIndex(x => new { x.OrderId, x.LineNumber }).IsUnique();
    }
}
