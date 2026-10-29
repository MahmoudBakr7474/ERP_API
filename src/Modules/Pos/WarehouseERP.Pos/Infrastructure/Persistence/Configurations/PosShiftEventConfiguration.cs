using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

internal sealed class PosShiftEventConfiguration : IEntityTypeConfiguration<PosShiftEvent>
{
    public void Configure(EntityTypeBuilder<PosShiftEvent> b)
    {
        b.ToTable("PosShiftEvents", "pos", t => t.HasCheckConstraint("CK_PosShiftEvents_Action", "[Action] BETWEEN 1 AND 5"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Action).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.Property(x => x.Note).HasMaxLength(PosShift.MaxNoteLength);
        b.Property(x => x.AtUtc).IsUtcTimestamp();
        b.HasIndex(x => x.ShiftId);
    }
}
