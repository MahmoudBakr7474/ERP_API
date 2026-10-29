using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// One row, seeded with the owner's V1 defaults (2026-10-29): a till discount of at most 10%, returns within 14 days.
internal sealed class PosSettingsConfiguration : IEntityTypeConfiguration<PosSettings>
{
    private static readonly DateTime Seeded = new(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<PosSettings> b)
    {
        b.ToTable("PosSettings", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosSettings_Single", $"[Id] = {PosSettings.SingletonId}");
            t.HasCheckConstraint("CK_PosSettings_MaxDiscount", "[MaxLineDiscountPercent] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_PosSettings_ReturnWindow", $"[ReturnWindowDays] BETWEEN 0 AND {PosSettings.MaxReturnWindowDays}");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.MaxLineDiscountPercent).HasPrecision(9, 4);
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasData(new { Id = PosSettings.SingletonId, MaxLineDiscountPercent = 10m, ReturnWindowDays = 14, CreatedAtUtc = Seeded });
    }
}
