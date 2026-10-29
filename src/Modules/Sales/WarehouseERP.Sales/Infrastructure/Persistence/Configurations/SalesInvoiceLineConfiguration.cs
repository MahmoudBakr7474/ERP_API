using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Foreign keys to Catalog live in Persistence.
internal sealed class SalesInvoiceLineConfiguration : IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> b)
    {
        PricedLineMapping.Map(b, "SalesInvoiceLines");
        b.HasOne<SalesOrderLine>().WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.InvoiceId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.OrderLineId);
    }
}
