using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

public static class TemporalMasterMapping
{
    public static void Map<TEntity>(EntityTypeBuilder<TEntity> builder, string table, string schema)
        where TEntity : MasterEntity
    {
        builder.ToTable(table, schema, tb => tb.IsTemporal(t =>
        {
            t.UseHistoryTable(table + "History", "history");
            t.HasPeriodStart("ValidFrom");
            t.HasPeriodEnd("ValidTo");
        }));
        builder.HasKey(x => x.Id);
        // SQL Server provider supplies a sequential Guid on Add for Guid PKs.
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.RowVersion).IsRowVersion();
        // No index on IsActive: a two-value column is never selective enough to be used.
        // User identifiers are nullable until Identity module is introduced.
    }
}
