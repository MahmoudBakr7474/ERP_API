using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

internal sealed class DocumentNumberSequenceConfiguration : IEntityTypeConfiguration<DocumentNumberSequence>
{
    public void Configure(EntityTypeBuilder<DocumentNumberSequence> b)
    {
        b.ToTable("DocumentNumberSequences", "accounting", t => t.HasCheckConstraint("CK_DocumentNumberSequences_Last", "[LastNumber] >= 0"));
        b.HasKey(x => new { x.JournalType, x.FiscalYearId });
        b.Property(x => x.JournalType).HasConversion<byte>().HasColumnType("tinyint");
        b.HasOne<FiscalYear>().WithMany().HasForeignKey(x => x.FiscalYearId).OnDelete(DeleteBehavior.Restrict);
    }
}
