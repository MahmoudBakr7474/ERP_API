using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// A return is saved only posted, refunded exactly what its lines are worth. Foreign keys to Organization and Inventory live
// in Persistence.
internal sealed class PosReturnConfiguration : IEntityTypeConfiguration<PosReturn>
{
    public void Configure(EntityTypeBuilder<PosReturn> b)
    {
        b.ToTable("PosReturns", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosReturns_Amounts",
                "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount] AND [CostAmount] >= 0"
                + " AND [CashRefundAmount] >= 0 AND [CardRefundAmount] >= 0 AND [CashRefundAmount] + [CardRefundAmount] = [TotalAmount]");
            t.HasCheckConstraint("CK_PosReturns_Posted", "[StockDocumentId] IS NOT NULL AND [StockDocumentNumber] IS NOT NULL");
            t.HasCheckConstraint("CK_PosReturns_PrintCount", "[PrintCount] >= 1");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.SaleNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.CustomerName).HasMaxLength(PosSale.MaxCustomerNameLength);
        b.Property(x => x.Reason).HasMaxLength(PosReturn.MaxReasonLength);
        b.Property(x => x.Notes).HasMaxLength(PosSale.MaxNotesLength);
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        b.Property(x => x.NetAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalAmount).HasPrecision(19, 4);
        b.Property(x => x.CostAmount).HasPrecision(19, 4);
        b.Property(x => x.CashRefundAmount).HasPrecision(19, 4);
        b.Property(x => x.CardRefundAmount).HasPrecision(19, 4);
        b.Property(x => x.StockDocumentNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.ReturnedAtUtc).IsUtcTimestamp();
        b.Property(x => x.LastPrintedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Refunds).WithOne().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<PosSale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PosTerminal>().WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PosShift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => new { x.TerminalId, x.FiscalYear, x.SequenceNumber }).IsUnique();
        b.HasIndex(x => x.StockDocumentId, "UX_PosReturns_StockDocument").IsUnique().HasFilter("[StockDocumentId] IS NOT NULL");
        b.HasIndex(x => x.SaleId);
        b.HasIndex(x => x.ShiftId);
        b.HasIndex(x => new { x.WarehouseId, x.ReturnedAtUtc });
    }
}
