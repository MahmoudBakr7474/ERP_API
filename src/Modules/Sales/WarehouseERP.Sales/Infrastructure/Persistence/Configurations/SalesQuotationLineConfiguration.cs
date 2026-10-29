using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Foreign keys to Catalog live in Persistence.
internal sealed class SalesQuotationLineConfiguration : IEntityTypeConfiguration<SalesQuotationLine>
{
    public void Configure(EntityTypeBuilder<SalesQuotationLine> b)
    {
        PricedLineMapping.Map(b, "SalesQuotationLines");
        b.HasIndex(x => new { x.QuotationId, x.LineNumber }).IsUnique();
    }
}
