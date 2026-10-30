using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// The database backs the domain's rules: every journal balances (debits = credits > 0); a posted one (3) has its number,
// fiscal year, period, sequence and time, and nothing else has a number; only a reversal (3) names the journal it
// reverses, and a journal has at most one reversal that isn't cancelled (4). Numbers are unique per type and fiscal year.
// Only vouchers (5–12) carry a cash account, party, method and amount; a supplier's bill number is used once.
internal sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> b)
    {
        b.ToTable("JournalEntries", "accounting", t =>
        {
            t.HasCheckConstraint("CK_JournalEntries_Type", $"[JournalType] BETWEEN 1 AND {Enum.GetValues<JournalType>().Length}");
            t.HasCheckConstraint("CK_JournalEntries_Status", "[Status] BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_JournalEntries_Balanced", "[TotalDebit] = [TotalCredit] AND [TotalDebit] > 0");
            t.HasCheckConstraint("CK_JournalEntries_Posted",
                "([Status] = 3 AND [Number] IS NOT NULL AND [FiscalYearId] IS NOT NULL AND [FiscalPeriodId] IS NOT NULL "
                + "AND [SequenceNumber] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) "
                + "OR ([Status] <> 3 AND [Number] IS NULL AND [PostedAtUtc] IS NULL)");
            t.HasCheckConstraint("CK_JournalEntries_Party", "([PartyType] IS NULL AND [PartyId] IS NULL) OR ([PartyType] IS NOT NULL AND [PartyId] IS NOT NULL)");
            t.HasCheckConstraint("CK_JournalEntries_Voucher",
                "[JournalType] BETWEEN 5 AND 12 OR ([CashAccountId] IS NULL AND [CounterCashAccountId] IS NULL AND [PartyId] IS NULL "
                + "AND [PaymentMethod] IS NULL AND [VoucherAmount] IS NULL)");
            t.HasCheckConstraint("CK_JournalEntries_Source",
                "([JournalType] BETWEEN 13 AND 16 AND [SourceType] IS NOT NULL AND [SourceId] IS NOT NULL) "
                + "OR ([JournalType] NOT BETWEEN 13 AND 16 AND [SourceType] IS NULL AND [SourceId] IS NULL)");
            t.HasCheckConstraint("CK_JournalEntries_Reversal",
                "([JournalType] = 3 AND [ReversalOfJournalId] IS NOT NULL) OR ([JournalType] <> 3 AND [ReversalOfJournalId] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.JournalType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Description).HasMaxLength(JournalEntry.MaxDescriptionLength);
        b.Property(x => x.Reference).HasMaxLength(JournalEntry.MaxReferenceLength);
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        b.Property(x => x.PartyType).HasConversion<byte?>().HasColumnType("tinyint");
        b.Property(x => x.PaymentMethod).HasConversion<byte?>().HasColumnType("tinyint");
        b.Property(x => x.SourceType).HasConversion<byte?>().HasColumnType("tinyint");
        b.Property(x => x.VoucherAmount).HasPrecision(19, 4);
        b.Property(x => x.WithholdingAmount).HasPrecision(19, 4);
        b.Property(x => x.TaxAmount).HasPrecision(19, 4);
        b.Property(x => x.TotalDebit).HasPrecision(19, 4);
        b.Property(x => x.TotalCredit).HasPrecision(19, 4);
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<CashAccount>().WithMany().HasForeignKey(x => x.CashAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CashAccount>().WithMany().HasForeignKey(x => x.CounterCashAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.ReversalOfJournalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FiscalYear>().WithMany().HasForeignKey(x => x.FiscalYearId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FiscalPeriod>().WithMany().HasForeignKey(x => x.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.JournalType, x.FiscalYearId, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        b.HasIndex(x => x.ReversalOfJournalId, "UX_JournalEntries_Reversal").IsUnique()
            .HasFilter("[ReversalOfJournalId] IS NOT NULL AND [Status] <> 4");
        b.HasIndex(x => new { x.Status, x.PostingDate }).IncludeProperties(x => x.JournalType);
        b.HasIndex(x => x.FiscalPeriodId);
        b.HasIndex(x => new { x.PartyId, x.Reference }, "UX_JournalEntries_BillNumber").IsUnique()
            .HasFilter("[JournalType] = 12 AND [Status] <> 4");
        b.HasIndex(x => x.CreatedAtUtc);
        b.HasIndex(x => new { x.SourceType, x.SourceId }, "UX_JournalEntries_Source").IsUnique().HasFilter("[SourceId] IS NOT NULL");
    }
}
