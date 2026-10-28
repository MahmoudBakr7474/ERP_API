using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// Posted when saved and never changed but for what returns take back. One receipt per stock document (and the stock
// document names the receipt back, unique too): a receipt can't be posted to stock twice.
internal sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> b)
    {
        b.ToTable("GoodsReceipts", "purchasing");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.SupplierDeliveryNote).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.StockDocumentNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.ReceivedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<PurchaseOrder>().WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique();
        b.HasIndex(x => x.StockDocumentId, "UX_GoodsReceipts_StockDocument").IsUnique();
        b.HasIndex(x => x.PurchaseOrderId);
        b.HasIndex(x => x.SupplierId);
        b.HasIndex(x => new { x.WarehouseId, x.ReceivedAtUtc });
    }
}
