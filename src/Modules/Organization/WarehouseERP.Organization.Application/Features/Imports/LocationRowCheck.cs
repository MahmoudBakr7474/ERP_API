using System.Text.RegularExpressions;
using WarehouseERP.Organization.Application.Contracts.Imports;

namespace WarehouseERP.Organization.Application.Features.Imports;

/// <summary>
/// Checks one row's text values and records every problem with its column, so the user sees all of a row's errors at
/// once. The same limits as the single-location validators: column lengths and the code's characters.
/// </summary>
public sealed class LocationRowCheck(List<LocationImportIssue> issues, int row)
{
    public bool Failed { get; private set; }

    public void Fail(string? field, string code, string message, string? value = null)
    {
        issues.Add(new LocationImportIssue(row, field, code, message, value));
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

    /// <summary>A required code made only of the pattern's characters.</summary>
    public string? Code(string field, string? value, int maxLength, Regex pattern, string formatMessage)
    {
        var code = Required(field, value, maxLength);
        if (code is not null && !pattern.IsMatch(code))
        {
            Fail(field, ImportIssueCodes.InvalidFormat, formatMessage, code);
            return null;
        }
        return code;
    }
}
