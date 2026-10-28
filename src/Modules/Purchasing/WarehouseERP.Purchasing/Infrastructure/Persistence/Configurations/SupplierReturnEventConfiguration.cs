using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class SupplierReturnEventConfiguration : IEntityTypeConfiguration<SupplierReturnEvent>
{
    public void Configure(EntityTypeBuilder<SupplierReturnEvent> b) => DocumentEventMapping.Map(b, "SupplierReturnEvents");
}
