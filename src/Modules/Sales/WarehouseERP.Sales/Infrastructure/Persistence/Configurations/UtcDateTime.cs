using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// datetime2 keeps no kind, so values read back as Unspecified and would reach JSON without "Z". Sales's timestamps are all
// UTC: stored as datetime2(3), read back as UTC.
internal static class UtcDateTime
{
    private static readonly ValueConverter<DateTime, DateTime> Required =
        new(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> Optional =
        new(value => value, value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

    public static PropertyBuilder IsUtcTimestamp(this PropertyBuilder property) =>
        property.HasColumnType("datetime2(3)")
            .HasConversion(property.Metadata.ClrType == typeof(DateTime?) ? Optional : Required);
}
