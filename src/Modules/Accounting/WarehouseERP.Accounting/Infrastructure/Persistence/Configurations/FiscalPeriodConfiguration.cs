using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// Twelve per year, numbered 1–12; no two periods start on the same day, so a date belongs to at most one period. A closed
// period (2) always has its closing time.
internal sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> b)
    {
        b.ToTable("FiscalPeriods", "accounting", t =>
        {
            t.HasCheckConstraint("CK_FiscalPeriods_Number", $"[Number] BETWEEN 1 AND {FiscalYear.PeriodCount}");
            t.HasCheckConstraint("CK_FiscalPeriods_Status", "[Status] BETWEEN 1 AND 2");
            t.HasCheckConstraint("CK_FiscalPeriods_Dates", "[EndDate] >= [StartDate]");
            t.HasCheckConstraint("CK_FiscalPeriods_Closed",
                "([Status] = 2 AND [ClosedAtUtc] IS NOT NULL) OR ([Status] = 1 AND [ClosedAtUtc] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.ClosedAtUtc).IsUtcTimestamp();
        b.HasIndex(x => new { x.FiscalYearId, x.Number }).IsUnique();
        b.HasIndex(x => x.StartDate, "UX_FiscalPeriods_StartDate").IsUnique().IncludeProperties(x => new { x.EndDate, x.Status });
    }
}
