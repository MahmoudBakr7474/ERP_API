using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

internal sealed class SalesReturnEventConfiguration : IEntityTypeConfiguration<SalesReturnEvent>
{
    public void Configure(EntityTypeBuilder<SalesReturnEvent> b) => DocumentEventMapping.Map(b, "SalesReturnEvents");
}
