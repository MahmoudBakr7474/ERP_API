using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// The owner's initial policy (V1 decisions): purchase requests and purchase orders need approval. Supplier returns
// always do and invoices do for price differences; neither has a row.
internal sealed class DocumentPolicyConfiguration : IEntityTypeConfiguration<DocumentPolicy>
{
    private static readonly DateTime Seeded = new(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<DocumentPolicy> b)
    {
        b.ToTable("DocumentPolicies", "purchasing", t => t.HasCheckConstraint("CK_DocumentPolicies_Type", "[DocumentType] IN (1, 2)"));
        b.HasKey(x => x.DocumentType);
        b.Property(x => x.DocumentType).HasConversion<byte>().HasColumnType("tinyint").ValueGeneratedNever();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasData(
            new { DocumentType = PurchasingDocumentType.PurchaseRequest, RequiresApproval = true, CreatedAtUtc = Seeded },
            new { DocumentType = PurchasingDocumentType.PurchaseOrder, RequiresApproval = true, CreatedAtUtc = Seeded });
    }
}
