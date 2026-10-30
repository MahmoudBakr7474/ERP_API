using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Imports;

/// <summary>The current database state an import is checked against.</summary>
public sealed record CatalogImportSnapshot(
    IReadOnlyList<Unit> Units, IReadOnlyList<Category> Categories, IReadOnlyList<Product> Products, IReadOnlyList<ExistingBarcode> Barcodes);
