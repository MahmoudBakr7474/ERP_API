using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderEventConfiguration : IEntityTypeConfiguration<PurchaseOrderEvent>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderEvent> b) => DocumentEventMapping.Map(b, "PurchaseOrderEvents");
}
