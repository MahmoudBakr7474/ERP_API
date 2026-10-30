using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Imports.CommitCatalogImport;

/// <param name="PreviewToken">The token of the preview the user confirmed.</param>
public sealed record CommitCatalogImportCommand(CatalogImportBatch Batch, CatalogImportMode Mode, string FileFingerprint, string PreviewToken);
