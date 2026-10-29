using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// Cash settles tendered less change; a card is externally confirmed, has its reference and gives no change. Only a card's
// last four digits are ever stored.
internal sealed class PosSalePaymentConfiguration : IEntityTypeConfiguration<PosSalePayment>
{
    public void Configure(EntityTypeBuilder<PosSalePayment> b)
    {
        b.ToTable("PosSalePayments", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosSalePayments_Amounts",
                "[Amount] >= 0 AND [ChangeAmount] >= 0 AND [Amount] = [TenderedAmount] - [ChangeAmount]");
            t.HasCheckConstraint("CK_PosSalePayments_Method",
                "([Method] = 1 AND [Status] = 1 AND [CardLast4] IS NULL)"
                + " OR ([Method] = 2 AND [Status] = 2 AND [Reference] IS NOT NULL AND [ChangeAmount] = 0 AND [Amount] > 0)");
            t.HasCheckConstraint("CK_PosSalePayments_CardLast4", "[CardLast4] IS NULL OR [CardLast4] LIKE '[0-9][0-9][0-9][0-9]'");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Method).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.Property(x => x.TenderedAmount).HasPrecision(19, 4);
        b.Property(x => x.ChangeAmount).HasPrecision(19, 4);
        b.Property(x => x.Reference).HasMaxLength(PosSale.MaxReferenceLength);
        b.Property(x => x.CardLast4).HasMaxLength(4).IsUnicode(false).IsFixedLength();
        b.HasIndex(x => new { x.SaleId, x.LineNumber }).IsUnique();
    }
}
