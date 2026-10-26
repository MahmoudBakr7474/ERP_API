using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

// The identity schema. ASP.NET Core Identity's own mapping (keys, lengths, unique UserNameIndex/RoleNameIndex)
// comes from IdentityDbContext; these configurations move its tables into the schema, add the ERP columns and
// Identity's own tables. Who could do what, and when, stays answerable: roles, permissions, role assignments and
// scope keep temporal history. Users don't (history would keep every old password hash), nor do sessions.
// The foreign keys from grants to organization.Branches/Warehouses are configured in ErpDbContext (ADR 0001).
internal static class IdentityTables
{
    public const string Schema = "identity";

    public static void ToTemporalTable<TEntity>(this EntityTypeBuilder<TEntity> builder, string table)
        where TEntity : class =>
        builder.ToTable(table, Schema, tb => tb.IsTemporal(t =>
        {
            t.UseHistoryTable(table + "History", "history");
            t.HasPeriodStart("ValidFrom");
            t.HasPeriodEnd("ValidTo");
        }));

    public static void MapAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(nameof(IAuditable.CreatedAtUtc)).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(nameof(IAuditable.UpdatedAtUtc)).HasColumnType("datetime2(3)");
    }
}
