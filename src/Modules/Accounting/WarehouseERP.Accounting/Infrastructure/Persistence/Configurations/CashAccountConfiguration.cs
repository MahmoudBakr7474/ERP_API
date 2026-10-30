using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// Cashboxes and bank accounts are master data (temporal). Unique code and one per ledger account; a cashbox has no bank
// details or overdraft. The branch foreign key lives in Persistence.
internal sealed class CashAccountConfiguration : IEntityTypeConfiguration<CashAccount>
{
    public void Configure(EntityTypeBuilder<CashAccount> b)
    {
        TemporalMasterMapping.Map(b, "CashAccounts", "accounting");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_CashAccounts_Kind", "[Kind] BETWEEN 1 AND 2");
            t.HasCheckConstraint("CK_CashAccounts_Cashbox",
                "[Kind] = 2 OR ([BankName] IS NULL AND [BankAccountNumber] IS NULL AND [AllowsOverdraft] = 0)");
            t.HasCheckConstraint("CK_CashAccounts_PosCash", "[ReceivesPosCash] = 0 OR [Kind] = 1");
        });
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.Code).HasMaxLength(CashAccount.MaxCodeLength).IsUnicode(false);
        b.Property(x => x.Name).HasMaxLength(CashAccount.MaxNameLength);
        b.Property(x => x.Kind).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.BankName).HasMaxLength(CashAccount.MaxBankNameLength);
        b.Property(x => x.BankAccountNumber).HasMaxLength(CashAccount.MaxAccountNumberLength);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Code, "UX_CashAccounts_Code").IsUnique();
        b.HasIndex(x => x.AccountId, "UX_CashAccounts_Account").IsUnique();
        b.Property(x => x.ReceivesPosCash).HasDefaultValue(false);
        b.HasIndex(x => x.BranchId);
        b.HasIndex(x => x.BranchId, "UX_CashAccounts_PosCash").IsUnique().HasFilter("[ReceivesPosCash] = 1");
    }
}
