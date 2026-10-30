using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// The chart is master data: a temporal table keeps every earlier version (and a deleted unused account). Codes are unique;
// a posting account has a section, only a posting account takes manual journals or a party, and a child points at a
// header of the chart (Restrict: a parent with children is never deleted).
internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        TemporalMasterMapping.Map(b, "Accounts", "accounting");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Accounts_Type", "[Type] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_Accounts_Section", "[Section] IS NULL OR [Section] BETWEEN 1 AND 11");
            t.HasCheckConstraint("CK_Accounts_PostingSection", "[IsPosting] = 0 OR [Section] IS NOT NULL");
            t.HasCheckConstraint("CK_Accounts_Manual", "[AllowsManualPosting] = 0 OR [IsPosting] = 1");
            t.HasCheckConstraint("CK_Accounts_Party", "[PartyType] IS NULL OR ([IsPosting] = 1 AND [PartyType] BETWEEN 1 AND 2)");
            t.HasCheckConstraint("CK_Accounts_Level", $"[Level] BETWEEN 1 AND {Account.MaxLevel}");
        });
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.Code).HasMaxLength(Account.MaxCodeLength).IsUnicode(false);
        b.Property(x => x.NameAr).HasMaxLength(Account.MaxNameLength);
        b.Property(x => x.NameEn).HasMaxLength(Account.MaxNameLength);
        b.Property(x => x.Type).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Section).HasConversion<byte?>().HasColumnType("tinyint");
        b.Property(x => x.PartyType).HasConversion<byte?>().HasColumnType("tinyint");
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Code, "UX_Accounts_Code").IsUnique();
        b.HasIndex(x => x.ParentId);
    }
}
