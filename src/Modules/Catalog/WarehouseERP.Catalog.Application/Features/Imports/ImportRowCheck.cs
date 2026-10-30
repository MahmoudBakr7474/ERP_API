using System.Globalization;
using System.Text.RegularExpressions;
using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>
/// Checks one row's text values and records every problem with its column, so the user sees all of a row's errors
/// at once. The same limits as the single-record validators: lengths of the columns, code characters, decimal(18,8).
/// </summary>
public sealed class ImportRowCheck(List<CatalogImportIssue> issues, CatalogImportSheet sheet, int row)
{
    public int Row => row;

    public bool Failed { get; private set; }

    public void Fail(string? field, string code, string message, string? value = null)
    {
        issues.Add(new CatalogImportIssue(sheet, row, field, code, message, value));
        Failed = true;
    }

    public string? Required(string field, string? value, int maxLength)
    {
        if (value is null)
        {
            Fail(field, ImportIssueCodes.Required, "A value is required.");
            return null;
        }
        return Optional(field, value, maxLength);
    }

    public string? Optional(string field, string? value, int maxLength)
    {
        if (value is not null && value.Length > maxLength)
        {
            Fail(field, ImportIssueCodes.TooLong, $"At most {maxLength} characters.", value[..Math.Min(value.Length, 60)]);
            return null;
        }
        return value;
    }

    /// <summary>A required code: letters, digits and the pattern's punctuation only.</summary>
    public string? Code(string field, string? value, int maxLength, Regex pattern) =>
        Pattern(field, Required(field, value, maxLength), pattern);

    public string? OptionalCode(string field, string? value, int maxLength, Regex pattern) =>
        Pattern(field, Optional(field, value, maxLength), pattern);

    public bool? Bool(string field, string? value)
    {
        switch (value?.ToUpperInvariant())
        {
            case null:
                Fail(field, ImportIssueCodes.Required, "A value is required: Yes or No.");
                return null;
            case "TRUE" or "YES" or "1" or "Y":
                return true;
            case "FALSE" or "NO" or "0" or "N":
                return false;
            default:
                Fail(field, ImportIssueCodes.InvalidValue, "Use Yes or No.", value);
                return null;
        }
    }

    /// <summary>An enum by its name (case ignored); numbers are refused so a typo can't pick a value by accident.</summary>
    public TEnum? Enum<TEnum>(string field, string? value, bool required) where TEnum : struct, Enum
    {
        if (value is null)
        {
            if (required)
                Fail(field, ImportIssueCodes.Required, $"A value is required: {string.Join(", ", System.Enum.GetNames<TEnum>())}.");
            return null;
        }
        if (!char.IsAsciiDigit(value[0]) && value[0] != '-' && System.Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            && System.Enum.IsDefined(parsed))
            return parsed;
        Fail(field, ImportIssueCodes.InvalidValue, $"Use one of: {string.Join(", ", System.Enum.GetNames<TEnum>())}.", value);
        return null;
    }

    /// <summary>A conversion factor: a positive number with a dot, at most 8 decimals and 10 whole digits.</summary>
    public decimal? Factor(string field, string? value)
    {
        if (value is null)
        {
            Fail(field, ImportIssueCodes.Required, "A value is required.");
            return null;
        }
        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                CultureInfo.InvariantCulture, out var factor)
            || factor <= 0 || factor.Scale > 8 && decimal.Round(factor, 8) != factor || decimal.Truncate(factor) >= 10_000_000_000m)
        {
            Fail(field, ImportIssueCodes.InvalidNumber,
                "Enter a positive number with a dot for decimals, at most 8 decimals and 10 whole digits.", value);
            return null;
        }
        return factor;
    }

    private string? Pattern(string field, string? value, Regex pattern)
    {
        if (value is not null && !pattern.IsMatch(value))
        {
            Fail(field, ImportIssueCodes.InvalidFormat, "Only English letters, digits and - _ (and . / in a SKU) are allowed.", value);
            return null;
        }
        return value;
    }
}
