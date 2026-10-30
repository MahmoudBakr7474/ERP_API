using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// An allocation is a positive amount between two items; a removed one keeps when and by whom.
internal sealed class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> b)
    {
        b.ToTable("Allocations", "accounting", t =>
        {
            t.HasCheckConstraint("CK_Allocations_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_Allocations_Items", "[DebitItemId] <> [CreditItemId]");
            t.HasCheckConstraint("CK_Allocations_Removed",
                "([IsActive] = 1 AND [RemovedAtUtc] IS NULL) OR ([IsActive] = 0 AND [RemovedAtUtc] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.PartyType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RemovedAtUtc).IsUtcTimestamp();
        b.HasOne<OpenItem>().WithMany().HasForeignKey(x => x.DebitItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OpenItem>().WithMany().HasForeignKey(x => x.CreditItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.DebitItemId);
        b.HasIndex(x => x.CreditItemId);
        b.HasIndex(x => new { x.PartyType, x.PartyId });
        b.HasIndex(x => x.JournalEntryId);
    }
}
