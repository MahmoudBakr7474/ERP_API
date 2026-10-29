using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

internal sealed class SalesQuotationEventConfiguration : IEntityTypeConfiguration<SalesQuotationEvent>
{
    public void Configure(EntityTypeBuilder<SalesQuotationEvent> b) => DocumentEventMapping.Map(b, "SalesQuotationEvents");
}
