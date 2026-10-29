using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

internal sealed class SalesOrderEventConfiguration : IEntityTypeConfiguration<SalesOrderEvent>
{
    public void Configure(EntityTypeBuilder<SalesOrderEvent> b) => DocumentEventMapping.Map(b, "SalesOrderEvents");
}
