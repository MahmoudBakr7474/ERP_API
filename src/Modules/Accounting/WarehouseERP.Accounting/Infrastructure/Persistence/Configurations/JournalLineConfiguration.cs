using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// A line has an amount on exactly one side, never negative; a party (with an optional due date) or none. An account with
// lines can't be deleted (Restrict); the branch foreign key lives in Persistence.
internal sealed class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> b)
    {
        b.ToTable("JournalLines", "accounting", t =>
        {
            t.HasCheckConstraint("CK_JournalLines_OneSide",
                "[Debit] >= 0 AND [Credit] >= 0 AND (([Debit] > 0 AND [Credit] = 0) OR ([Debit] = 0 AND [Credit] > 0))");
            t.HasCheckConstraint("CK_JournalLines_Party",
                "([PartyType] IS NULL AND [PartyId] IS NULL AND [DueDate] IS NULL) OR ([PartyType] BETWEEN 1 AND 2 AND [PartyId] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Debit).HasPrecision(19, 4);
        b.Property(x => x.Credit).HasPrecision(19, 4);
        b.Property(x => x.Description).HasMaxLength(JournalLine.MaxDescriptionLength);
        b.Property(x => x.PartyType).HasConversion<byte?>().HasColumnType("tinyint");
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.JournalEntryId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.AccountId).IncludeProperties(x => new { x.JournalEntryId, x.BranchId, x.Debit, x.Credit });
        b.HasIndex(x => x.BranchId);
        b.HasIndex(x => new { x.PartyType, x.PartyId }).HasFilter("[PartyId] IS NOT NULL");
    }
}
