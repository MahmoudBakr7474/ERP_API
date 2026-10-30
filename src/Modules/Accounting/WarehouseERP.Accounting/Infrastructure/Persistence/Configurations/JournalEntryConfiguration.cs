using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// The database backs the domain's rules: every journal balances (debits = credits > 0); a posted one (3) has its number,
// fiscal year, period, sequence and time, and nothing else has a number; only a reversal (3) names the journal it
// reverses, and a journal has at most one reversal that isn't cancelled (4). Numbers are unique per type and fiscal year.
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
        b.Property(x => x.TotalDebit).HasPrecision(19, 4);
        b.Property(x => x.TotalCredit).HasPrecision(19, 4);
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
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
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
