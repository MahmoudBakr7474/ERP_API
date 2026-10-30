using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// One draft (1) per bank account at a time; a completed one (2) keeps when and what it opened from.
internal sealed class BankReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
{
    public void Configure(EntityTypeBuilder<BankReconciliation> b)
    {
        b.ToTable("BankReconciliations", "accounting", t =>
        {
            t.HasCheckConstraint("CK_BankReconciliations_Status", "[Status] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_BankReconciliations_Completed",
                "([Status] = 2 AND [CompletedAtUtc] IS NOT NULL AND [OpeningClearedBalance] IS NOT NULL) OR ([Status] <> 2 AND [CompletedAtUtc] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.StatementBalance).HasPrecision(19, 4);
        b.Property(x => x.OpeningClearedBalance).HasPrecision(19, 4);
        b.Property(x => x.CompletedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.BankReconciliationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<CashAccount>().WithMany().HasForeignKey(x => x.CashAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CashAccountId, "UX_BankReconciliations_Draft").IsUnique().HasFilter("[Status] = 1");
        b.HasIndex(x => new { x.CashAccountId, x.StatementDate });
    }
}
