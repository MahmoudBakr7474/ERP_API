using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// The database guards what the request checks: orders never take more of a line than was approved.
internal sealed class PurchaseRequestLineConfiguration : IEntityTypeConfiguration<PurchaseRequestLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestLine> b)
    {
        b.ToTable("PurchaseRequestLines", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_PurchaseRequestLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
            t.HasCheckConstraint("CK_PurchaseRequestLines_Ordered", "[OrderedQuantity] >= 0 AND [OrderedQuantity] <= [Quantity]");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.OrderedQuantity).HasPrecision(18, 4);
        b.Property(x => x.Notes).HasMaxLength(200);
        b.HasIndex(x => new { x.RequestId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.ProductId);
    }
}
