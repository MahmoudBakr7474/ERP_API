using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportDataQualityIssues;

/// <summary>The data-quality report: one row per issue, with the report screen's filters.</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportDataQualityIssuesQuery(CatalogDataIssue? Issue, string? Search, bool? ProductIsActive, int MaxRows);
