using WarehouseERP.Catalog.Application.Contracts.Imports;

namespace WarehouseERP.Catalog.Presentation.Imports;

/// <summary>An uploaded file's rows, and the fingerprint that binds a preview to this exact file.</summary>
internal sealed record CatalogImportUpload(CatalogImportBatch Batch, string Fingerprint);
