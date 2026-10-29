using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// One row, seeded with the owner's V1 defaults (2026-10-29): neither quotations nor orders need approval, no discount
// threshold, manual and below-list prices need no approval, a credit hold blocks confirmation and deliveries, standard VAT
// 14%, quotations valid 14 days.
internal sealed class SalesSettingsConfiguration : IEntityTypeConfiguration<SalesSettings>
{
    private static readonly DateTime Seeded = new(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<SalesSettings> b)
    {
        b.ToTable("SalesSettings", "sales", t =>
        {
            t.HasCheckConstraint("CK_SalesSettings_Single", $"[Id] = {SalesSettings.SingletonId}");
            t.HasCheckConstraint("CK_SalesSettings_DiscountThreshold",
                "[DiscountApprovalThreshold] IS NULL OR ([DiscountApprovalThreshold] >= 0 AND [DiscountApprovalThreshold] < 100)");
            t.HasCheckConstraint("CK_SalesSettings_CreditHoldPolicy", "[CreditHoldPolicy] IN (1, 2)");
            t.HasCheckConstraint("CK_SalesSettings_DefaultVatRate", "[DefaultVatRate] > 0 AND [DefaultVatRate] <= 100");
            t.HasCheckConstraint("CK_SalesSettings_QuotationValidity", "[QuotationValidityDays] BETWEEN 1 AND 365");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.DiscountApprovalThreshold).HasPrecision(9, 4);
        b.Property(x => x.CreditHoldPolicy).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.DefaultVatRate).HasPrecision(9, 4);
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasData(new
        {
            Id = SalesSettings.SingletonId, QuotationsRequireApproval = false, OrdersRequireApproval = false,
            ManualPriceRequiresApproval = false, PriceBelowListRequiresApproval = false,
            CreditHoldPolicy = CreditHoldPolicy.BlockConfirmation, CreditHoldBlocksDeliveries = true, DefaultVatRate = 14m,
            QuotationValidityDays = 14, CreatedAtUtc = Seeded,
        });
    }
}
