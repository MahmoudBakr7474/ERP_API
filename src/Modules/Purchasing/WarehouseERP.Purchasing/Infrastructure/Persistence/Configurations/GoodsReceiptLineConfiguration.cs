using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// A receipt line never gives back more than it brought in.
internal sealed class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> b)
    {
        b.ToTable("GoodsReceiptLines", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_GoodsReceiptLines_Quantity", "[Quantity] > 0 AND [UnitCost] >= 0");
            t.HasCheckConstraint("CK_GoodsReceiptLines_Returned", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.UnitCost).HasPrecision(19, 8);
        b.Property(x => x.ReturnedQuantity).HasPrecision(18, 4);
        b.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ReceiptId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.PurchaseOrderLineId);
    }
}
