using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseRequestEventConfiguration : IEntityTypeConfiguration<PurchaseRequestEvent>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestEvent> b) => DocumentEventMapping.Map(b, "PurchaseRequestEvents");
}
