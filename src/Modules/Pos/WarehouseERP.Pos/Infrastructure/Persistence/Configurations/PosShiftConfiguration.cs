using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// One open shift per terminal and per cashier (filtered unique indexes), a drawer that never goes below zero, and a closed
// shift always has its count, expected cash and difference. Foreign keys to Organization live in Persistence.
internal sealed class PosShiftConfiguration : IEntityTypeConfiguration<PosShift>
{
    public void Configure(EntityTypeBuilder<PosShift> b)
    {
        b.ToTable("PosShifts", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosShifts_Status", "[Status] IN (1, 2)");
            t.HasCheckConstraint("CK_PosShifts_Closed",
                "([Status] = 2 AND [ClosedAtUtc] IS NOT NULL AND [CountedCash] IS NOT NULL AND [ExpectedCashAtClose] IS NOT NULL"
                + " AND [CashDifference] = [CountedCash] - [ExpectedCashAtClose])"
                + " OR ([Status] = 1 AND [ClosedAtUtc] IS NULL AND [CountedCash] IS NULL AND [CashDifference] IS NULL)");
            t.HasCheckConstraint("CK_PosShifts_Amounts",
                "[OpeningFloat] >= 0 AND [CashSalesAmount] >= 0 AND [CashRefundsAmount] >= 0 AND [CashInAmount] >= 0"
                + " AND [CashOutAmount] >= 0 AND [CardSalesAmount] >= 0 AND [CardRefundsAmount] >= 0 AND [SalesAmount] >= 0"
                + " AND [RefundsAmount] >= 0 AND [SalesCount] >= 0 AND [ReturnsCount] >= 0");
            t.HasCheckConstraint("CK_PosShifts_Drawer",
                "[OpeningFloat] + [CashSalesAmount] - [CashRefundsAmount] + [CashInAmount] - [CashOutAmount] >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        foreach (var amount in new[]
                 {
                     nameof(PosShift.OpeningFloat), nameof(PosShift.CashSalesAmount), nameof(PosShift.CashRefundsAmount),
                     nameof(PosShift.CashInAmount), nameof(PosShift.CashOutAmount), nameof(PosShift.CardSalesAmount),
                     nameof(PosShift.CardRefundsAmount), nameof(PosShift.SalesAmount), nameof(PosShift.RefundsAmount),
                     nameof(PosShift.CountedCash), nameof(PosShift.ExpectedCashAtClose), nameof(PosShift.CashDifference),
                 })
            b.Property(amount).HasPrecision(19, 4);
        b.Property(x => x.ClosingNote).HasMaxLength(PosShift.MaxNoteLength);
        b.Property(x => x.OpenedAtUtc).IsUtcTimestamp();
        b.Property(x => x.ClosedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Ignore(x => x.ExpectedCash);
        b.Ignore(x => x.IsOpen);

        b.HasMany(x => x.CashMovements).WithOne().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<PosTerminal>().WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => new { x.TerminalId, x.FiscalYear, x.SequenceNumber }).IsUnique();
        b.HasIndex(x => x.TerminalId, "UX_PosShifts_OpenTerminal").IsUnique().HasFilter("[Status] = 1");
        b.HasIndex(x => x.CashierUserId, "UX_PosShifts_OpenCashier").IsUnique().HasFilter("[Status] = 1");
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.OpenedAtUtc);
    }
}
