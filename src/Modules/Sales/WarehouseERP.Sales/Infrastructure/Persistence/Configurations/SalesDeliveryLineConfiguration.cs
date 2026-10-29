using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// A delivery line never takes back more than it delivered. Foreign keys to Catalog, Organization and Inventory's stock
// document lines live in Persistence.
internal sealed class SalesDeliveryLineConfiguration : IEntityTypeConfiguration<SalesDeliveryLine>
{
    public void Configure(EntityTypeBuilder<SalesDeliveryLine> b)
    {
        b.ToTable("SalesDeliveryLines", "sales", t =>
        {
            t.HasCheckConstraint("CK_SalesDeliveryLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
            t.HasCheckConstraint("CK_SalesDeliveryLines_Returned", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
            t.HasCheckConstraint("CK_SalesDeliveryLines_Cost", "[CostAmount] IS NULL OR [CostAmount] >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.CostAmount).HasPrecision(19, 4);
        b.Property(x => x.ReturnedQuantity).HasPrecision(18, 4);
        b.Ignore(x => x.BaseQuantity);
        b.Ignore(x => x.ReturnableQuantity);
        b.HasOne<SalesOrderLine>().WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.DeliveryId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.OrderLineId);
        b.HasIndex(x => x.StockLineId).IsUnique().HasFilter("[StockLineId] IS NOT NULL");
    }
}
