using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Operational data, not master data: no temporal table; its history is SalesQuotationEvents. An issued, converted or
// declined quotation always has its number. Foreign keys to Organization live in Persistence.
internal sealed class SalesQuotationConfiguration : IEntityTypeConfiguration<SalesQuotation>
{
    public void Configure(EntityTypeBuilder<SalesQuotation> b)
    {
        b.ToTable("SalesQuotations", "sales", t =>
        {
            t.HasCheckConstraint("CK_SalesQuotations_Status", "[Status] BETWEEN 1 AND 6");
            t.HasCheckConstraint("CK_SalesQuotations_Number",
                "([Status] IN (3, 4, 5) AND [Number] IS NOT NULL AND [SequenceNumber] IS NOT NULL)"
                + " OR ([Status] IN (1, 2) AND [Number] IS NULL) OR [Status] = 6");
            t.HasCheckConstraint("CK_SalesQuotations_Validity", "[ValidUntil] >= [QuotationDate]");
            t.HasCheckConstraint("CK_SalesQuotations_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
            t.HasCheckConstraint("CK_SalesQuotations_Converted",
                "([Status] = 4 AND [ConvertedOrderId] IS NOT NULL) OR ([Status] <> 4 AND [ConvertedOrderId] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        b.Property(x => x.CustomerReference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.DeclinedReason).HasMaxLength(500);
        b.Property(x => x.GrossAmount).HasPrecision(19, 4);
        b.Property(x => x.DiscountAmount).HasPrecision(19, 4);
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.ApprovedAtUtc).IsUtcTimestamp();
        b.Property(x => x.IssuedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.ConvertedOrderId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        // A quotation becomes one order, and an order comes from at most one quotation.
        b.HasIndex(x => x.ConvertedOrderId).IsUnique().HasFilter("[ConvertedOrderId] IS NOT NULL");
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
