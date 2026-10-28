using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// Operational data, not master data: no temporal table; its history is PurchaseOrderEvents. An approved, received or
// closed order always has its number. Foreign keys to Organization live in Persistence.
internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.ToTable("PurchaseOrders", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_PurchaseOrders_Status", "[Status] BETWEEN 1 AND 7");
            t.HasCheckConstraint("CK_PurchaseOrders_Number",
                "([Status] IN (3, 4, 5, 6) AND [Number] IS NOT NULL AND [SequenceNumber] IS NOT NULL)"
                + " OR ([Status] IN (1, 2) AND [Number] IS NULL) OR [Status] = 7");
            t.HasCheckConstraint("CK_PurchaseOrders_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        b.Property(x => x.SupplierReference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.ClosedReason).HasMaxLength(500);
        b.Property(x => x.GrossAmount).HasPrecision(19, 4);
        b.Property(x => x.DiscountAmount).HasPrecision(19, 4);
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.ApprovedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.SupplierId);
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
