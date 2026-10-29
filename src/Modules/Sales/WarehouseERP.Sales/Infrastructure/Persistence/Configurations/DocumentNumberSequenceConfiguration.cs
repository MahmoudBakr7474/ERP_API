using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

internal sealed class DocumentNumberSequenceConfiguration : IEntityTypeConfiguration<DocumentNumberSequence>
{
    public void Configure(EntityTypeBuilder<DocumentNumberSequence> b)
    {
        b.ToTable("DocumentNumberSequences", "sales", t =>
        {
            t.HasCheckConstraint("CK_DocumentNumberSequences_LastNumber", "[LastNumber] > 0");
            t.HasCheckConstraint("CK_DocumentNumberSequences_Type", "[DocumentType] BETWEEN 1 AND 5");
        });
        b.HasKey(x => new { x.DocumentType, x.BranchId, x.FiscalYear });
        b.Property(x => x.DocumentType).HasConversion<byte>().HasColumnType("tinyint");
    }
}
