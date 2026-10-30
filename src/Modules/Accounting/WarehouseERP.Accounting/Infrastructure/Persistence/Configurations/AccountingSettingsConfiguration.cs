using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// One row, versioned in a temporal table (who changed the start date and when stays visible), seeded with the owner's V1
// defaults (2026-10-30): calendar fiscal years, accounting starting 2027-01-01 (configurable until a journal posts) and
// manual journals that need an approver.
internal sealed class AccountingSettingsConfiguration : IEntityTypeConfiguration<AccountingSettings>
{
    private static readonly DateTime Seeded = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<AccountingSettings> b)
    {
        b.ToTable("AccountingSettings", "accounting", t =>
        {
            t.IsTemporal(h =>
            {
                h.UseHistoryTable("AccountingSettingsHistory", "history");
                h.HasPeriodStart("ValidFrom");
                h.HasPeriodEnd("ValidTo");
            });
            t.HasCheckConstraint("CK_AccountingSettings_Single", $"[Id] = {AccountingSettings.SingletonId}");
            t.HasCheckConstraint("CK_AccountingSettings_StartMonth", "[FiscalYearStartMonth] BETWEEN 1 AND 12");
            t.HasCheckConstraint("CK_AccountingSettings_Cutover", "DAY([CutoverDate]) = 1");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Property(x => x.VouchersRequireApproval).HasDefaultValue(false);
        b.HasData(new
        {
            Id = AccountingSettings.SingletonId,
            FiscalYearStartMonth = 1,
            CutoverDate = new DateOnly(2027, 1, 1),
            ManualJournalsRequireApproval = true,
            VouchersRequireApproval = false,
            CreatedAtUtc = Seeded,
        });
    }
}
