using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// The initial policy the owner approved (V1_DECISIONS.md): opening stock and adjustments need approval, receipts and
// issues post directly; transfer dispatches post directly too. A reversal always needs approval and has no row; a
// transfer receipt is never approved and a count adjustment is approved on its count.
internal sealed class DocumentTypePolicyConfiguration : IEntityTypeConfiguration<DocumentTypePolicy>
{
    private static readonly DateTime Seeded = new(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<DocumentTypePolicy> b)
    {
        b.ToTable("DocumentTypePolicies", "inventory", t => t.HasCheckConstraint("CK_DocumentTypePolicies_Type", "[DocumentType] IN (1, 2, 3, 4, 6)"));
        b.HasKey(x => x.DocumentType);
        b.Property(x => x.DocumentType).HasConversion<byte>().HasColumnType("tinyint").ValueGeneratedNever();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasData(
            new { DocumentType = StockDocumentType.OpeningBalance, RequiresApproval = true, CreatedAtUtc = Seeded },
            new { DocumentType = StockDocumentType.Receipt, RequiresApproval = false, CreatedAtUtc = Seeded },
            new { DocumentType = StockDocumentType.Issue, RequiresApproval = false, CreatedAtUtc = Seeded },
            new { DocumentType = StockDocumentType.Adjustment, RequiresApproval = true, CreatedAtUtc = Seeded },
            new { DocumentType = StockDocumentType.TransferDispatch, RequiresApproval = false, CreatedAtUtc = Seeded });
    }
}
