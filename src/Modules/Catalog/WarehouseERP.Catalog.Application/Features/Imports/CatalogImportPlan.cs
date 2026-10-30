using System.Security.Cryptography;
using System.Text;
using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>What an import will do with every row, and every problem found. Built the same way for the preview and
/// for the commit, which compares the two through <see cref="Token"/>.</summary>
public sealed class CatalogImportPlan(CatalogImportMode mode)
{
    public const int MaxIssuesShown = 1_000;

    public CatalogImportMode Mode => mode;

    public List<PlannedUnit> Units { get; } = [];

    public List<PlannedCategory> Categories { get; } = [];

    public List<PlannedProduct> Products { get; } = [];

    public List<PlannedProductUnit> ProductUnits { get; } = [];

    public List<PlannedBarcode> Barcodes { get; } = [];

    public List<CatalogImportIssue> Issues { get; } = [];

    public List<CatalogImportRowResult> Rows { get; } = [];

    /// <summary>Rows whose key appears more than once in their sheet.</summary>
    public HashSet<(CatalogImportSheet Sheet, int Row)> Duplicates { get; } = [];

    /// <summary>All or nothing: no problem anywhere, and something to create or change.</summary>
    public bool CanCommit => Issues.Count == 0 && Rows.Any(r => r.Action is CatalogImportAction.Create or CatalogImportAction.Update);

    public CatalogImportCounts Totals => Count(Rows);

    public IReadOnlyList<CatalogImportSheetSummary> Sheets =>
        System.Enum.GetValues<CatalogImportSheet>()
            .Where(s => Rows.Any(r => r.Sheet == s))
            .Select(s => new CatalogImportSheetSummary(s, Count(Rows.Where(r => r.Sheet == s))))
            .ToList();

    /// <summary>
    /// Identifies this exact outcome: the file's fingerprint, the mode and every row's action. A commit whose token
    /// differs from the preview's would do something else than what the user confirmed.
    /// </summary>
    public string Token(string fileFingerprint)
    {
        var text = new StringBuilder(fileFingerprint).Append('|').Append(mode);
        foreach (var row in Rows)
            text.Append('|').Append((int)row.Sheet).Append(':').Append(row.Row).Append(':').Append((int)row.Action);
        text.Append("|issues:").Append(Issues.Count);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..32];
    }

    public CatalogImportPreviewResponse ToPreview(string fileFingerprint, int maxIssues = MaxIssuesShown) => new(
        mode, Token(fileFingerprint), CanCommit, Totals, Sheets, Rows, Issues.Take(maxIssues).ToList(), Issues.Count,
        Issues.Count > maxIssues);

    private CatalogImportCounts Count(IEnumerable<CatalogImportRowResult> rows)
    {
        var list = rows.ToList();
        int Of(CatalogImportAction action) => list.Count(r => r.Action == action);
        var invalid = Of(CatalogImportAction.Error);
        return new CatalogImportCounts(
            list.Count, list.Count - invalid, invalid, list.Count(r => Duplicates.Contains((r.Sheet, r.Row))),
            Of(CatalogImportAction.Create), Of(CatalogImportAction.Update), Of(CatalogImportAction.Unchanged), Of(CatalogImportAction.Skip));
    }
}
