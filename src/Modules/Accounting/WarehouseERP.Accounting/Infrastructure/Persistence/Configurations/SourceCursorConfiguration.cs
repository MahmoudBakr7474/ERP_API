using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

internal sealed class SourceCursorConfiguration : IEntityTypeConfiguration<SourceCursor>
{
    public void Configure(EntityTypeBuilder<SourceCursor> b)
    {
        b.ToTable("SourceCursors", "accounting");
        b.HasKey(x => x.Module);
        b.Property(x => x.Module).HasMaxLength(SourceCursor.MaxModuleLength).IsUnicode(false);
        b.Property(x => x.ScannedToUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
    }
}
