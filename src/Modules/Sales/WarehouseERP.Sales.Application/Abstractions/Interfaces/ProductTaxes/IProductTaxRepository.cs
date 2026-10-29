using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;

/// <summary>Write side of products' tax classifications, and the active ones tills price with.</summary>
public interface IProductTaxRepository
{
    /// <summary>The active classification of each of these products that has one, by product.</summary>
    Task<IReadOnlyDictionary<Guid, ProductTax>> FindActiveAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct);

    /// <summary>The product's classification row, active or removed, tracked; null when it never had one.</summary>
    Task<ProductTax?> GetForUpdateAsync(Guid productId, CancellationToken ct);

    void Add(ProductTax tax);

    /// <summary>The save point; a product classified twice at once comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
