using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

internal sealed class PosNumberSequenceConfiguration : IEntityTypeConfiguration<PosNumberSequence>
{
    public void Configure(EntityTypeBuilder<PosNumberSequence> b)
    {
        b.ToTable("PosNumberSequences", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosNumberSequences_LastNumber", "[LastNumber] > 0");
            t.HasCheckConstraint("CK_PosNumberSequences_Type", "[DocumentType] BETWEEN 1 AND 3");
        });
        b.HasKey(x => new { x.DocumentType, x.TerminalId, x.FiscalYear });
        b.Property(x => x.DocumentType).HasConversion<byte>().HasColumnType("tinyint");
        b.HasOne<PosTerminal>().WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Restrict);
    }
}
