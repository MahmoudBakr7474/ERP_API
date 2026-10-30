using WarehouseERP.Organization.Application.Contracts.Imports;

namespace WarehouseERP.Organization.Presentation.Imports;

/// <summary>An uploaded file's rows, and the fingerprint that binds a preview to this exact file.</summary>
internal sealed record LocationImportUpload(LocationImportBatch Batch, string Fingerprint);
