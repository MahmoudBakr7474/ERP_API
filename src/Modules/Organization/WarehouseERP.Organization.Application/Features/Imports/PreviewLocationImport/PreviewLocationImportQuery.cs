using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Imports.PreviewLocationImport;

/// <param name="FileFingerprint">A hash of the uploaded bytes, so the preview token is bound to this exact file.</param>
/// <param name="AllIssues">Every issue instead of the first 1,000 (for the downloadable error report).</param>
public sealed record PreviewLocationImportQuery(
    LocationImportBatch Batch, LocationImportMode Mode, string FileFingerprint, bool AllIssues = false);
