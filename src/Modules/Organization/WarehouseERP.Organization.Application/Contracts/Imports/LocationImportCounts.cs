namespace WarehouseERP.Organization.Application.Contracts.Imports;

/// <summary>
/// Row counts of a location import. <paramref name="Valid"/> + <paramref name="Invalid"/> = <paramref name="Rows"/>;
/// <paramref name="Duplicates"/> counts invalid rows whose location appears more than once in the file;
/// Create + Update + Unchanged + Skip = Valid.
/// </summary>
public sealed record LocationImportCounts(int Rows, int Valid, int Invalid, int Duplicates, int Create, int Update, int Unchanged, int Skip);
