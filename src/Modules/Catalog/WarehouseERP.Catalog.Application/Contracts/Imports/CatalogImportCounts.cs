namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>
/// Row counts of an import (one sheet or all). <paramref name="Valid"/> + <paramref name="Invalid"/> =
/// <paramref name="Rows"/>; <paramref name="Duplicates"/> counts invalid rows whose key appears more than once in
/// their sheet; Create + Update + Unchanged + Skip = Valid.
/// </summary>
public sealed record CatalogImportCounts(
    int Rows, int Valid, int Invalid, int Duplicates, int Create, int Update, int Unchanged, int Skip);
