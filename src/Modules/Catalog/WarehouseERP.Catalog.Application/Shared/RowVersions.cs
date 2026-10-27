using FluentValidation;

namespace WarehouseERP.Catalog.Application.Shared;

// Optimistic concurrency on master records: a client sends back the base64 row version it read, and a stale one
// comes back as Common.ConcurrencyConflict (409). A bad one is RowVersionErrors.Invalid.
internal static class RowVersions
{
    // SQL Server rowversion: 8 bytes, sent to clients as base64.
    public static bool TryParse(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrEmpty(value)) return false;
        var buffer = new byte[8];
        if (!Convert.TryFromBase64String(value, buffer, out var written) || written != 8) return false;
        rowVersion = buffer;
        return true;
    }

    public static IRuleBuilderOptions<T, string> MustBeRowVersion<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(v => TryParse(v, out _))
            .WithErrorCode("RowVersionValidator")
            .WithMessage("'Row Version' must be the value read with the record.");
}
