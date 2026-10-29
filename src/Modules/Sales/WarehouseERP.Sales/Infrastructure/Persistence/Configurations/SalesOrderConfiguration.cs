using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Operational data, not master data: no temporal table; its history is SalesOrderEvents. A confirmed, delivered or
// closed order always has its number. Foreign keys to Organization live in Persistence.
internal sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> b)
    {
        b.ToTable("SalesOrders", "sales", t =>
        {
            t.HasCheckConstraint("CK_SalesOrders_Status", "[Status] BETWEEN 1 AND 7");
            t.HasCheckConstraint("CK_SalesOrders_Number",
                "([Status] IN (3, 4, 5, 6) AND [Number] IS NOT NULL AND [SequenceNumber] IS NOT NULL)"
                + " OR ([Status] IN (1, 2) AND [Number] IS NULL) OR [Status] = 7");
            t.HasCheckConstraint("CK_SalesOrders_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
            t.HasCheckConstraint("CK_SalesOrders_Amounts", "[InvoicedAmount] >= 0 AND [CreditedAmount] >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        b.Property(x => x.CustomerReference).HasMaxLength(100);
        b.Property(x => x.ShippingAddress).HasMaxLength(500);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.ClosedReason).HasMaxLength(500);
        b.Property(x => x.GrossAmount).HasPrecision(19, 4);
        b.Property(x => x.DiscountAmount).HasPrecision(19, 4);
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.InvoicedAmount).HasPrecision(19, 4);
        b.Property(x => x.CreditedAmount).HasPrecision(19, 4);
        b.Property(x => x.ApprovedAtUtc).IsUtcTimestamp();
        b.Property(x => x.ConfirmedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SalesQuotation>().WithMany().HasForeignKey(x => x.SourceQuotationId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => new { x.CustomerId, x.Status });
        b.HasIndex(x => x.SourceQuotationId);
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
