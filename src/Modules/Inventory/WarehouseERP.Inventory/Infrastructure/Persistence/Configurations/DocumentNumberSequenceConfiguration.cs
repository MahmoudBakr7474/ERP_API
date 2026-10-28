using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class DocumentNumberSequenceConfiguration : IEntityTypeConfiguration<DocumentNumberSequence>
{
    public void Configure(EntityTypeBuilder<DocumentNumberSequence> b)
    {
        b.ToTable("DocumentNumberSequences", "inventory", t => t.HasCheckConstraint("CK_DocumentNumberSequences_LastNumber", "[LastNumber] > 0"));
        b.HasKey(x => new { x.DocumentType, x.BranchId, x.FiscalYear });
        b.Property(x => x.DocumentType).HasConversion<byte>().HasColumnType("tinyint");
    }
}
