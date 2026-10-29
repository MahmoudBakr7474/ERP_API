using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

internal sealed class PosRefundConfiguration : IEntityTypeConfiguration<PosRefund>
{
    public void Configure(EntityTypeBuilder<PosRefund> b)
    {
        b.ToTable("PosRefunds", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosRefunds_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_PosRefunds_Method",
                "([Method] = 1 AND [Status] = 1 AND [CardLast4] IS NULL) OR ([Method] = 2 AND [Status] = 2 AND [Reference] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Method).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.Property(x => x.Reference).HasMaxLength(PosSale.MaxReferenceLength);
        b.Property(x => x.CardLast4).HasMaxLength(4).IsUnicode(false).IsFixedLength();
        b.HasIndex(x => new { x.ReturnId, x.LineNumber }).IsUnique();
    }
}
