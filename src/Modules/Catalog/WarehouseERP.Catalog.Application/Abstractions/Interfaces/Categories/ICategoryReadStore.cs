using WarehouseERP.Catalog.Application.Contracts.Categories;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;

/// <summary>Read side of categories: projections, no tracking.</summary>
public interface ICategoryReadStore
{
    Task<CategoryDetailsResponse?> GetAsync(Guid categoryId, CancellationToken ct);

    /// <summary>Every category (or only active or inactive ones), flat, with its parent id; clients build the tree.</summary>
    Task<IReadOnlyList<CategoryResponse>> ListAsync(bool? isActive, CancellationToken ct);
}
