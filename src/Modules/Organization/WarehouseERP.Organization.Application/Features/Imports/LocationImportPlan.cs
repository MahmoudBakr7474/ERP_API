using System.Security.Cryptography;
using System.Text;
using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Imports;

/// <summary>What a location import will do with every row, and every problem found. Built the same way for the preview
/// and for the commit, which compares the two through <see cref="Token"/>.</summary>
public sealed class LocationImportPlan(LocationImportMode mode)
{
    public const int MaxIssuesShown = 1_000;

    public LocationImportMode Mode => mode;

    public List<PlannedLocation> Locations { get; } = [];

    public List<LocationImportIssue> Issues { get; } = [];

    public List<LocationImportRowResult> Rows { get; } = [];

    /// <summary>Rows whose location appears more than once in the file.</summary>
    public HashSet<int> Duplicates { get; } = [];

    /// <summary>All or nothing: no problem anywhere, and something to create or change.</summary>
    public bool CanCommit => Issues.Count == 0 && Rows.Any(r => r.Action is LocationImportAction.Create or LocationImportAction.Update);

    public LocationImportCounts Totals
    {
        get
        {
            int Of(LocationImportAction action) => Rows.Count(r => r.Action == action);
            var invalid = Of(LocationImportAction.Error);
            return new LocationImportCounts(
                Rows.Count, Rows.Count - invalid, invalid, Rows.Count(r => Duplicates.Contains(r.Row)),
                Of(LocationImportAction.Create), Of(LocationImportAction.Update), Of(LocationImportAction.Unchanged), Of(LocationImportAction.Skip));
        }
    }

    /// <summary>
    /// Identifies this exact outcome: the file's fingerprint, the mode and every row's action. A commit whose token
    /// differs from the preview's would do something else than what the user confirmed.
    /// </summary>
    public string Token(string fileFingerprint)
    {
        var text = new StringBuilder(fileFingerprint).Append('|').Append(mode);
        foreach (var row in Rows)
            text.Append('|').Append(row.Row).Append(':').Append((int)row.Action);
        text.Append("|issues:").Append(Issues.Count);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..32];
    }

    public LocationImportPreviewResponse ToPreview(string fileFingerprint, int maxIssues = MaxIssuesShown) => new(
        mode, Token(fileFingerprint), CanCommit, Totals, Rows, Issues.Take(maxIssues).ToList(), Issues.Count, Issues.Count > maxIssues);
}
