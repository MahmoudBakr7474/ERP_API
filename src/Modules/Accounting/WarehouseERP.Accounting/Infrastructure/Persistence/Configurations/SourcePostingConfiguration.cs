using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

internal sealed class SourcePostingConfiguration : IEntityTypeConfiguration<SourcePosting>
{
    public void Configure(EntityTypeBuilder<SourcePosting> b)
    {
        b.ToTable("SourcePostings", "accounting", t =>
        {
            t.HasCheckConstraint("CK_SourcePostings_Type", $"[SourceType] BETWEEN 1 AND {Enum.GetValues<SourceDocumentType>().Length}");
            t.HasCheckConstraint("CK_SourcePostings_Status", $"[Status] BETWEEN 1 AND {Enum.GetValues<SourcePostingStatus>().Length}");
            t.HasCheckConstraint("CK_SourcePostings_Posted",
                "([Status] = 2 AND [JournalId] IS NOT NULL AND [JournalNumber] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) "
                + "OR ([Status] <> 2 AND [JournalId] IS NULL AND [JournalNumber] IS NULL)");
            t.HasCheckConstraint("CK_SourcePostings_Next",
                "([Status] IN (1, 3) AND [NextAttemptAtUtc] IS NOT NULL) OR ([Status] IN (2, 4) AND [NextAttemptAtUtc] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.SourceType).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.SourceNumber).HasMaxLength(SourcePosting.MaxNumberLength);
        b.Property(x => x.JournalNumber).HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.ErrorCode).HasMaxLength(SourcePosting.MaxErrorCodeLength).IsUnicode(false);
        b.Property(x => x.ErrorMessage).HasMaxLength(SourcePosting.MaxErrorMessageLength);
        b.Property(x => x.StockValue).HasPrecision(19, 4);
        b.Property(x => x.PostedStockValue).HasPrecision(19, 4);
        b.Property(x => x.VarianceAmount).HasPrecision(19, 4);
        b.Property(x => x.OccurredAtUtc).IsUtcTimestamp();
        b.Property(x => x.LastAttemptAtUtc).IsUtcTimestamp();
        b.Property(x => x.NextAttemptAtUtc).IsUtcTimestamp();
        b.Property(x => x.DiscoveredAtUtc).IsUtcTimestamp();
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SourceType, x.SourceId }, "UX_SourcePostings_Source").IsUnique();
        b.HasIndex(x => new { x.Status, x.NextAttemptAtUtc }).IncludeProperties(x => x.OccurredAtUtc);
        b.HasIndex(x => x.OccurredAtUtc);
        b.HasIndex(x => x.JournalId).IsUnique().HasFilter("[JournalId] IS NOT NULL");
    }
}
