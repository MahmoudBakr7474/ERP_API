using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence.Configurations;

// An approved, ordered or closed request (3, 4, 5) always has its number.
internal sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> b)
    {
        b.ToTable("PurchaseRequests", "purchasing", t =>
        {
            t.HasCheckConstraint("CK_PurchaseRequests_Status", "[Status] BETWEEN 1 AND 6");
            t.HasCheckConstraint("CK_PurchaseRequests_Number",
                "([Status] IN (3, 4, 5) AND [Number] IS NOT NULL AND [SequenceNumber] IS NOT NULL)"
                + " OR ([Status] IN (1, 2) AND [Number] IS NULL) OR [Status] = 6");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.ClosedReason).HasMaxLength(500);
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
