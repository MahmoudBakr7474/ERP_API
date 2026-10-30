using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Imports;

/// <summary>
/// The import's unit of work over the shared DbContext: it loads what a batch refers to (tracked, so an update saves
/// against the row versions it read), takes the new records, and saves everything in one transaction.
/// </summary>
public interface ICatalogImportStore
{
    /// <summary>Every unit and category, the products with these SKUs (with their units and barcodes), and the barcodes
    /// with these values wherever they are.</summary>
    Task<CatalogImportSnapshot> LoadAsync(IReadOnlyCollection<string> skus, IReadOnlyCollection<string> barcodes, CancellationToken ct);

    /// <summary>Tracks a new record; its id is assigned now, so later rows can refer to it.</summary>
    void Add(Unit unit);

    void Add(Category category);

    void Add(Product product);

    /// <summary>The save point: one SaveChanges, all or nothing. Unique keys taken meanwhile and changed rows map to errors.</summary>
    Task<Result> SaveAsync(CancellationToken ct);
}
