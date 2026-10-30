using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// One item per posted control-account line (unique line), never zero; its open amount keeps the amount's sign and never
// passes zero or the amount. The line, journal and account it comes from are never deleted (Restrict).
internal sealed class OpenItemConfiguration : IEntityTypeConfiguration<OpenItem>
{
    public void Configure(EntityTypeBuilder<OpenItem> b)
    {
        b.ToTable("OpenItems", "accounting", t =>
        {
            t.HasCheckConstraint("CK_OpenItems_PartyType", "[PartyType] BETWEEN 1 AND 2");
            t.HasCheckConstraint("CK_OpenItems_Open",
                "([Amount] > 0 AND [OpenAmount] BETWEEN 0 AND [Amount]) OR ([Amount] < 0 AND [OpenAmount] BETWEEN [Amount] AND 0)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.PartyType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.JournalType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.DocumentNumber).HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Reference).HasMaxLength(JournalEntry.MaxReferenceLength);
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.Property(x => x.OpenAmount).HasPrecision(19, 4);
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.HasOne<JournalLine>().WithMany().HasForeignKey(x => x.JournalLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.JournalLineId, "UX_OpenItems_Line").IsUnique();
        b.HasIndex(x => x.JournalEntryId);
        b.HasIndex(x => new { x.PartyType, x.PartyId, x.DueDate });
        b.HasIndex(x => x.AccountId);
    }
}
