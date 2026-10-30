using System.ComponentModel.DataAnnotations;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>Bound to the <c>Documents</c> configuration section (docs/DOCUMENTS.md).</summary>
public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    /// <summary>
    /// The QuestPDF license the organisation that owns this deployment holds: Community only when it is eligible
    /// (annual gross revenue under USD 1M, not publicly traded or public sector), otherwise Professional or Enterprise.
    /// Required, so nobody generates PDFs under a license they never chose.
    /// </summary>
    [Required]
    public PdfLicense? PdfLicense { get; init; }

    /// <summary>Optional SVG logo printed on documents instead of the default brand logo (path relative to the host).</summary>
    public string? LogoPath { get; init; }

    /// <summary>Most rows one XLSX or CSV export may hold; a larger result is refused with a clear error.</summary>
    [Range(1, 200_000)]
    public int MaxExportRows { get; init; } = 25_000;

    /// <summary>Most rows one tabular PDF may hold (about 30 per page); larger results go to Excel.</summary>
    [Range(1, 50_000)]
    public int MaxPdfRows { get; init; } = 5_000;
}
