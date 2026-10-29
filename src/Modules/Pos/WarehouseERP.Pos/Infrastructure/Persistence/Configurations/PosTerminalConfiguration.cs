using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// Terminals are master data: a temporal table keeps every earlier setting. Unique code; at least one payment method.
// Foreign keys to Organization live in Persistence.
internal sealed class PosTerminalConfiguration : IEntityTypeConfiguration<PosTerminal>
{
    public void Configure(EntityTypeBuilder<PosTerminal> b)
    {
        TemporalMasterMapping.Map(b, "PosTerminals", "pos");
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.ToTable(t => t.HasCheckConstraint("CK_PosTerminals_PaymentMethod", "[AcceptsCash] = 1 OR [AcceptsCard] = 1"));
        b.Property(x => x.Code).HasMaxLength(PosTerminal.MaxCodeLength).IsUnicode(false);
        b.Property(x => x.Name).HasMaxLength(PosTerminal.MaxNameLength);
        b.Property(x => x.ReceiptHeader).HasMaxLength(PosTerminal.MaxReceiptTextLength);
        b.Property(x => x.ReceiptFooter).HasMaxLength(PosTerminal.MaxReceiptTextLength);
        b.HasIndex(x => x.Code, "UX_PosTerminals_Code").IsUnique();
        b.HasIndex(x => x.WarehouseId);
    }
}
