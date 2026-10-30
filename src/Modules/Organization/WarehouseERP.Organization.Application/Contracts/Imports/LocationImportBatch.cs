namespace WarehouseERP.Organization.Application.Contracts.Imports;

/// <param name="FileIssues">Problems of the file itself found while reading it (a missing column).</param>
public sealed record LocationImportBatch(IReadOnlyList<LocationImportRow> Rows, IReadOnlyList<LocationImportIssue> FileIssues);
