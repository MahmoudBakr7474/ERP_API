using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// Operational data, not master data: no temporal table. The document's own history is StockDocumentEvents and, once
// posted, its movements and cost entries. Foreign keys to Organization live in Persistence.
internal sealed class StockDocumentConfiguration : IEntityTypeConfiguration<StockDocument>
{
    public void Configure(EntityTypeBuilder<StockDocument> b)
    {
        b.ToTable("StockDocuments", "inventory", t =>
        {
            t.HasCheckConstraint("CK_StockDocuments_Type", "[DocumentType] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_StockDocuments_Status", "[Status] BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_StockDocuments_Posted",
                "([Status] = 3 AND [Number] IS NOT NULL AND [FiscalYear] IS NOT NULL AND [SequenceNumber] IS NOT NULL AND [PostedAtUtc] IS NOT NULL)"
                + " OR ([Status] <> 3 AND [Number] IS NULL AND [PostedAtUtc] IS NULL)");
            t.HasCheckConstraint("CK_StockDocuments_Reason",
                "([DocumentType] = 4 AND [AdjustmentReason] IS NOT NULL) OR ([DocumentType] <> 4 AND [AdjustmentReason] IS NULL)");
            t.HasCheckConstraint("CK_StockDocuments_Reversal",
                "([DocumentType] = 5 AND [ReversalOfDocumentId] IS NOT NULL AND [RequiresApproval] = 1)"
                + " OR ([DocumentType] <> 5 AND [ReversalOfDocumentId] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        // The draft reference's number; an identity column, so a rolled-back draft leaves a gap and nothing else.
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.DocumentType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.AdjustmentReason).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.ExternalReference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.PostedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.ReversalOfDocumentId).OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.DocumentType, x.BranchId, x.FiscalYear, x.SequenceNumber })
            .IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        // One reversal per document, unless it was cancelled.
        b.HasIndex(x => x.ReversalOfDocumentId, "UX_StockDocuments_OneReversal")
            .IsUnique().HasFilter("[ReversalOfDocumentId] IS NOT NULL AND [Status] <> 4");
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
