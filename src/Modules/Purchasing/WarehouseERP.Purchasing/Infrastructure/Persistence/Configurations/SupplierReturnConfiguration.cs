using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// A posted return (3) always has its number and the stock document Inventory posted for it, unique (and the stock
// document names the return back): a return can't leave stock twice.
internal sealed class SupplierReturnConfiguration : IEntityTypeConfiguration<SupplierReturn>
{
    public void Configure(EntityTypeBuilder<SupplierReturn> b)
    {
        b.ToTable("SupplierReturns", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_SupplierReturns_Status", "[Status] BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_SupplierReturns_Posted",
                "([Status] = 3 AND [Number] IS NOT NULL AND [StockDocumentId] IS NOT NULL AND [PostedAtUtc] IS NOT NULL)"
                + " OR ([Status] <> 3 AND [Number] IS NULL AND [StockDocumentId] IS NULL)");
            t.HasCheckConstraint("CK_SupplierReturns_Claim",
                "[CreditClaimNetAmount] >= 0 AND [CreditClaimTaxAmount] >= 0"
                + " AND [CreditClaimTotalAmount] = [CreditClaimNetAmount] + [CreditClaimTaxAmount]");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.StockDocumentNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.CreditClaimNetAmount).HasPrecision(19, 4);
        b.Property(x => x.CreditClaimTaxAmount).HasPrecision(19, 4);
        b.Property(x => x.CreditClaimTotalAmount).HasPrecision(19, 4);
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<GoodsReceipt>().WithMany().HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PurchaseOrder>().WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        b.HasIndex(x => x.StockDocumentId, "UX_SupplierReturns_StockDocument").IsUnique().HasFilter("[StockDocumentId] IS NOT NULL");
        b.HasIndex(x => x.GoodsReceiptId);
        b.HasIndex(x => x.PurchaseOrderId);
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
