using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// A sale is saved only posted (its stock document), with totals that add up, payments that pay exactly its total (cash
// received = tendered − change, change only out of cash, cards never above the total) and refunds never above what was
// paid each way. Foreign keys to Organization, Sales' customers and Inventory live in Persistence.
internal sealed class PosSaleConfiguration : IEntityTypeConfiguration<PosSale>
{
    public void Configure(EntityTypeBuilder<PosSale> b)
    {
        b.ToTable("PosSales", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosSales_Amounts",
                "[GrossAmount] >= 0 AND [DiscountAmount] >= 0 AND [NetAmount] = [GrossAmount] - [DiscountAmount] AND [TaxAmount] >= 0"
                + " AND [TotalAmount] = [NetAmount] + [TaxAmount] AND [CostAmount] >= 0");
            t.HasCheckConstraint("CK_PosSales_Payments",
                "[CashTenderedAmount] >= 0 AND [ChangeAmount] >= 0 AND [ChangeAmount] <= [CashTenderedAmount]"
                + " AND [CashReceivedAmount] = [CashTenderedAmount] - [ChangeAmount] AND [CardAmount] >= 0 AND [CardAmount] <= [TotalAmount]"
                + " AND [CashReceivedAmount] + [CardAmount] = [TotalAmount]");
            t.HasCheckConstraint("CK_PosSales_Refunds",
                "[RefundedCashAmount] >= 0 AND [RefundedCashAmount] <= [CashReceivedAmount]"
                + " AND [RefundedCardAmount] >= 0 AND [RefundedCardAmount] <= [CardAmount] AND [ReturnCount] >= 0");
            t.HasCheckConstraint("CK_PosSales_Posted", "[StockDocumentId] IS NOT NULL AND [StockDocumentNumber] IS NOT NULL");
            t.HasCheckConstraint("CK_PosSales_PrintCount", "[PrintCount] >= 1");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.CustomerName).HasMaxLength(PosSale.MaxCustomerNameLength);
        b.Property(x => x.CustomerPhone).HasMaxLength(PosSale.MaxPhoneLength);
        b.Property(x => x.CustomerTaxNumber).HasMaxLength(50);
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        foreach (var amount in new[]
                 {
                     nameof(PosSale.GrossAmount), nameof(PosSale.DiscountAmount), nameof(PosSale.NetAmount), nameof(PosSale.TaxAmount),
                     nameof(PosSale.TotalAmount), nameof(PosSale.CostAmount), nameof(PosSale.CashTenderedAmount),
                     nameof(PosSale.ChangeAmount), nameof(PosSale.CashReceivedAmount), nameof(PosSale.CardAmount),
                     nameof(PosSale.RefundedCashAmount), nameof(PosSale.RefundedCardAmount),
                 })
            b.Property(amount).HasPrecision(19, 4);
        b.Property(x => x.StockDocumentNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Notes).HasMaxLength(PosSale.MaxNotesLength);
        b.Property(x => x.SoldAtUtc).IsUtcTimestamp();
        b.Property(x => x.LastPrintedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Ignore(x => x.RefundedAmount);

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<PosTerminal>().WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PosShift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => new { x.TerminalId, x.FiscalYear, x.SequenceNumber }).IsUnique();
        b.HasIndex(x => x.StockDocumentId, "UX_PosSales_StockDocument").IsUnique().HasFilter("[StockDocumentId] IS NOT NULL");
        b.HasIndex(x => x.ShiftId);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => new { x.WarehouseId, x.SoldAtUtc });
        b.HasIndex(x => x.SoldAtUtc);
    }
}
