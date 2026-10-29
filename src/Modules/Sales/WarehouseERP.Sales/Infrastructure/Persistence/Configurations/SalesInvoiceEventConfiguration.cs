using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

internal sealed class SalesInvoiceEventConfiguration : IEntityTypeConfiguration<SalesInvoiceEvent>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceEvent> b) => DocumentEventMapping.Map(b, "SalesInvoiceEvents");
}
