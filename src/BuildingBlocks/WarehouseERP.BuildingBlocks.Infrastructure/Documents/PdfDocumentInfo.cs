namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>What a branded PDF states in its header.</summary>
/// <param name="Details">First-page label/value lines: filters, period, warehouse or branch.</param>
/// <param name="Summary">First-page figures (counts, totals) shown as small boxes.</param>
/// <param name="DocumentNumber">The business document's number, when the PDF is one document.</param>
/// <param name="DocumentDate">The business document's date, already formatted.</param>
public sealed record PdfDocumentInfo(
    string Title,
    IReadOnlyList<ReportFilter>? Details = null,
    IReadOnlyList<ReportFilter>? Summary = null,
    string? DocumentNumber = null,
    string? DocumentDate = null,
    bool Landscape = false);
