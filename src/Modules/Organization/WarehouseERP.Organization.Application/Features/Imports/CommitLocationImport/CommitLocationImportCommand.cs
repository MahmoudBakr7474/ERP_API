using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Imports.CommitLocationImport;

/// <param name="PreviewToken">The token of the preview the user confirmed.</param>
public sealed record CommitLocationImportCommand(
    LocationImportBatch Batch, LocationImportMode Mode, string FileFingerprint, string PreviewToken);
