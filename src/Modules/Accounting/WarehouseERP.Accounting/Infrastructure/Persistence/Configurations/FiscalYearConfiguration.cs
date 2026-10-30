using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// A year starts once (unique start date) and a closed year (2) always has its closing time. Its periods and history go
// with it; its closing journal is a posted journal of the ledger.
internal sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> b)
    {
        b.ToTable("FiscalYears", "accounting", t =>
        {
            t.HasCheckConstraint("CK_FiscalYears_Status", "[Status] BETWEEN 1 AND 2");
            t.HasCheckConstraint("CK_FiscalYears_Dates", "[EndDate] > [StartDate]");
            t.HasCheckConstraint("CK_FiscalYears_Closed",
                "([Status] = 2 AND [ClosedAtUtc] IS NOT NULL) OR ([Status] = 1 AND [ClosedAtUtc] IS NULL AND [ClosingJournalId] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Name).HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.ClosedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasMany(x => x.Periods).WithOne().HasForeignKey(x => x.FiscalYearId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.FiscalYearId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.ClosingJournalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.StartDate, "UX_FiscalYears_StartDate").IsUnique();
        b.HasIndex(x => x.ClosingJournalId);
    }
}
