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
            t.HasCheckConstraint("CK_StockDocuments_Type", "[DocumentType] BETWEEN 1 AND 14");
            t.HasCheckConstraint("CK_StockDocuments_Status", "[Status] BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_StockDocuments_Posted",
                "([Status] = 3 AND [Number] IS NOT NULL AND [FiscalYear] IS NOT NULL AND [SequenceNumber] IS NOT NULL AND [PostedAtUtc] IS NOT NULL)"
                + " OR ([Status] <> 3 AND [Number] IS NULL AND [PostedAtUtc] IS NULL)");
            t.HasCheckConstraint("CK_StockDocuments_Reason",
                "([DocumentType] = 4 AND [AdjustmentReason] IS NOT NULL) OR ([DocumentType] <> 4 AND [AdjustmentReason] IS NULL)");
            t.HasCheckConstraint("CK_StockDocuments_Reversal",
                "([DocumentType] = 5 AND [ReversalOfDocumentId] IS NOT NULL AND [RequiresApproval] = 1)"
                + " OR ([DocumentType] <> 5 AND [ReversalOfDocumentId] IS NULL)");
            // Purchase receipts and returns (9, 10), sales deliveries and returns (11, 12) and POS sales and returns (13, 14)
            // always name the Purchasing, Sales or POS document that posted them; nothing else does.
            t.HasCheckConstraint("CK_StockDocuments_Source",
                "([DocumentType] IN (9, 10, 11, 12, 13, 14) AND [SourceDocumentId] IS NOT NULL)"
                + " OR ([DocumentType] NOT IN (9, 10, 11, 12, 13, 14) AND [SourceDocumentId] IS NULL)");
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
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.ReversalOfDocumentId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<StockTransfer>().WithMany().HasForeignKey(x => x.TransferId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<StockCount>().WithMany().HasForeignKey(x => x.CountId).OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.DocumentType, x.BranchId, x.FiscalYear, x.SequenceNumber })
            .IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        // One reversal per document, unless it was cancelled.
        b.HasIndex(x => x.ReversalOfDocumentId, "UX_StockDocuments_OneReversal")
            .IsUnique().HasFilter("[ReversalOfDocumentId] IS NOT NULL AND [Status] <> 4");
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.TransferId);
        b.HasIndex(x => x.CountId);
        // One stock document per Purchasing goods receipt or supplier return, and per Sales delivery or customer return.
        b.HasIndex(x => x.SourceDocumentId, "UX_StockDocuments_Source").IsUnique().HasFilter("[SourceDocumentId] IS NOT NULL");
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
