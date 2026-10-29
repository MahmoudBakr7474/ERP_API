using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

internal sealed class PosCashMovementConfiguration : IEntityTypeConfiguration<PosCashMovement>
{
    public void Configure(EntityTypeBuilder<PosCashMovement> b)
    {
        b.ToTable("PosCashMovements", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosCashMovements_Type", "[MovementType] IN (1, 2)");
            t.HasCheckConstraint("CK_PosCashMovements_Amount", "[Amount] > 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.MovementType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.Property(x => x.Reason).HasMaxLength(PosShift.MaxReasonLength);
        b.Property(x => x.AtUtc).IsUtcTimestamp();
        b.HasIndex(x => x.ShiftId);
    }
}
