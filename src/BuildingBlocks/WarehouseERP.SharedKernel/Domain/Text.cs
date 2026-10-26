namespace WarehouseERP.SharedKernel.Domain;

public static class Text
{
    /// <summary>Trimmed text, or null when blank: an optional field is either a value or absent.</summary>
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
