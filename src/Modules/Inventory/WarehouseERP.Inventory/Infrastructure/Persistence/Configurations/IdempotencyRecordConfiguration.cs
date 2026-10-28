using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> b)
    {
        b.ToTable("IdempotencyRecords", "inventory");
        b.HasKey(x => x.Key);
        b.Property(x => x.Key).ValueGeneratedNever();
        b.Property(x => x.Operation).HasMaxLength(40).IsUnicode(false);
        b.Property(x => x.Fingerprint).HasMaxLength(64).IsUnicode(false).IsFixedLength();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => x.DocumentId);
        b.HasIndex(x => x.SubjectId);
    }
}
