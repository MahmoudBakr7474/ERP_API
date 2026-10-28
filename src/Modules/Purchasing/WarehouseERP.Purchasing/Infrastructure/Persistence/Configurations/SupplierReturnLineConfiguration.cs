using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// Once posted, a line's invoiced and uninvoiced parts add up to its quantity (both are zero before).
internal sealed class SupplierReturnLineConfiguration : IEntityTypeConfiguration<SupplierReturnLine>
{
    public void Configure(EntityTypeBuilder<SupplierReturnLine> b)
    {
        b.ToTable("SupplierReturnLines", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_SupplierReturnLines_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_SupplierReturnLines_Split",
                "[UninvoicedQuantity] >= 0 AND [InvoicedQuantity] >= 0"
                + " AND ([UninvoicedQuantity] + [InvoicedQuantity] = 0 OR [UninvoicedQuantity] + [InvoicedQuantity] = [Quantity])");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.ReceiptUnitCost).HasPrecision(19, 8);
        b.Property(x => x.UninvoicedQuantity).HasPrecision(18, 4);
        b.Property(x => x.InvoicedQuantity).HasPrecision(18, 4);
        b.Property(x => x.CreditClaimNetAmount).HasPrecision(19, 4);
        b.Property(x => x.CreditClaimTaxAmount).HasPrecision(19, 4);
        b.HasOne<GoodsReceiptLine>().WithMany().HasForeignKey(x => x.GoodsReceiptLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ReturnId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.GoodsReceiptLineId);
        b.HasIndex(x => x.PurchaseOrderLineId);
    }
}
