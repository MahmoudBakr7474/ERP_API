using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// One row per mapped role, versioned in a temporal table like master data, so every earlier mapping can be traced. An
// account a role uses can't be deleted (Restrict).
internal sealed class AccountMappingConfiguration : IEntityTypeConfiguration<AccountMapping>
{
    public void Configure(EntityTypeBuilder<AccountMapping> b)
    {
        b.ToTable("AccountMappings", "accounting", t =>
        {
            t.IsTemporal(h =>
            {
                h.UseHistoryTable("AccountMappingsHistory", "history");
                h.HasPeriodStart("ValidFrom");
                h.HasPeriodEnd("ValidTo");
            });
            t.HasCheckConstraint("CK_AccountMappings_Role", $"[Role] BETWEEN 1 AND {Enum.GetValues<PostingRole>().Length}");
        });
        b.HasKey(x => x.Role);
        b.Property(x => x.Role).HasConversion<byte>().HasColumnType("tinyint").ValueGeneratedNever();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.AccountId);
    }
}
