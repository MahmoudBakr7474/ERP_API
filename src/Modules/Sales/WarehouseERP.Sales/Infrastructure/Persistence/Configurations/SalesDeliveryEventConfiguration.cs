using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

internal sealed class SalesDeliveryEventConfiguration : IEntityTypeConfiguration<SalesDeliveryEvent>
{
    public void Configure(EntityTypeBuilder<SalesDeliveryEvent> b) => DocumentEventMapping.Map(b, "SalesDeliveryEvents");
}
