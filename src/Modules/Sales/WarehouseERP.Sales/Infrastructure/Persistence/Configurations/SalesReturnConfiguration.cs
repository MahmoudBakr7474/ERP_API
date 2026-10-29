using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// An approved return always has its number and its stock document (one return per stock document); its pending credit
// is never negative. Foreign keys to Organization and Inventory live in Persistence.
internal sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> b)
    {
        b.ToTable("SalesReturns", "sales", t =>
        {
            t.HasCheckConstraint("CK_SalesReturns_Status", "[Status] BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_SalesReturns_Approved",
                "([Status] = 3 AND [Number] IS NOT NULL AND [StockDocumentId] IS NOT NULL AND [ApprovedAtUtc] IS NOT NULL)"
                + " OR ([Status] <> 3 AND [Number] IS NULL AND [StockDocumentId] IS NULL)");
            t.HasCheckConstraint("CK_SalesReturns_Amounts",
                "[CostAmount] >= 0 AND [CreditNetAmount] >= 0 AND [CreditTaxAmount] >= 0"
                + " AND [CreditTotalAmount] = [CreditNetAmount] + [CreditTaxAmount]");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.StockDocumentNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.CostAmount).HasPrecision(19, 4);
        b.Property(x => x.CreditNetAmount).HasPrecision(19, 4);
        b.Property(x => x.CreditTaxAmount).HasPrecision(19, 4);
        b.Property(x => x.CreditTotalAmount).HasPrecision(19, 4);
        b.Property(x => x.ApprovedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<SalesDelivery>().WithMany().HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        b.HasIndex(x => x.StockDocumentId, "UX_SalesReturns_StockDocument").IsUnique().HasFilter("[StockDocumentId] IS NOT NULL");
        b.HasIndex(x => x.DeliveryId);
        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
