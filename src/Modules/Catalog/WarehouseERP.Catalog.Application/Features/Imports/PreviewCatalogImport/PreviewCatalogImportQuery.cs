using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Imports.PreviewCatalogImport;

/// <param name="FileFingerprint">A hash of the uploaded bytes, so the preview token is bound to this exact file.</param>
/// <param name="AllIssues">Every issue instead of the first 1,000 (for the downloadable error report).</param>
public sealed record PreviewCatalogImportQuery(
    CatalogImportBatch Batch, CatalogImportMode Mode, string FileFingerprint, bool AllIssues = false);
