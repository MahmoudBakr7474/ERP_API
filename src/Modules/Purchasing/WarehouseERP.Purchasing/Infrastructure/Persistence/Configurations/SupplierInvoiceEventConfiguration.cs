using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class SupplierInvoiceEventConfiguration : IEntityTypeConfiguration<SupplierInvoiceEvent>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceEvent> b) => DocumentEventMapping.Map(b, "SupplierInvoiceEvents");
}
