using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

internal sealed class JournalEventConfiguration : IEntityTypeConfiguration<JournalEvent>
{
    public void Configure(EntityTypeBuilder<JournalEvent> b)
    {
        b.ToTable("JournalEvents", "accounting",
            t => t.HasCheckConstraint("CK_JournalEvents_Action", $"[Action] BETWEEN 1 AND {Enum.GetValues<JournalAction>().Length}"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Action).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.AtUtc).IsUtcTimestamp();
        b.Property(x => x.Note).HasMaxLength(JournalEntry.MaxNoteLength);
        b.HasIndex(x => x.JournalEntryId);
    }
}
