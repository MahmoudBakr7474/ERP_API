namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// A report as rows and columns, already in one language, ready for <see cref="DocumentFiles"/> to write as XLSX, CSV
/// or PDF. The owning module selects, filters and totals the data; the writers only lay it out, so the three formats
/// always hold the same values.
/// </summary>
/// <param name="FileName">ASCII base name of the download, e.g. <c>catalog-products</c>; a timestamp is appended.</param>
/// <param name="Rows">One value per column: string, number, bool or DateTime (DateTime already in company time).</param>
/// <param name="Totals">Optional closing row, one value per column (null leaves a cell empty).</param>
/// <param name="Summary">Optional label/value lines printed above the table (counts, totals).</param>
public sealed record TabularReport(
    DocumentLanguage Language,
    string FileName,
    string Title,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    IReadOnlyList<ReportFilter> Filters,
    bool Landscape = false,
    IReadOnlyList<object?>? Totals = null,
    IReadOnlyList<ReportFilter>? Summary = null);
