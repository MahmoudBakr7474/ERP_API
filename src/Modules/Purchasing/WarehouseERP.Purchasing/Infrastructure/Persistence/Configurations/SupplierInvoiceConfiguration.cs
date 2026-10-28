using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// A supplier's invoice number is unique per supplier once normalized, among invoices that are not cancelled (4). A
// posted invoice (3) always has its company number.
internal sealed class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
{
    public void Configure(EntityTypeBuilder<SupplierInvoice> b)
    {
        b.ToTable("SupplierInvoices", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_SupplierInvoices_Status", "[Status] BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_SupplierInvoices_Posted",
                "([Status] = 3 AND [Number] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [Number] IS NULL)");
            t.HasCheckConstraint("CK_SupplierInvoices_Dates", "[DueDate] >= [InvoiceDate]");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        b.Property(x => x.SupplierInvoiceNumber).HasMaxLength(SupplierInvoiceNumbers.MaxLength);
        b.Property(x => x.SupplierInvoiceNumberKey).HasMaxLength(SupplierInvoiceNumbers.MaxLength);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.GrossAmount).HasPrecision(19, 4);
        b.Property(x => x.DiscountAmount).HasPrecision(19, 4);
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.PriceVarianceAmount).HasPrecision(19, 4);
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<PurchaseOrder>().WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        b.HasIndex(x => new { x.SupplierId, x.SupplierInvoiceNumberKey }, "UX_SupplierInvoices_SupplierNumber")
            .IsUnique().HasFilter("[Status] <> 4");
        b.HasIndex(x => x.PurchaseOrderId);
        b.HasIndex(x => new { x.Status, x.DueDate });
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
