using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;
using WarehouseERP.Catalog.Application.Contracts.Categories;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Categories.ListCategories;

// The whole (small) category list in one call, so a client can draw the tree.
public sealed class ListCategoriesHandler(ICategoryReadStore categories)
    : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> HandleAsync(ListCategoriesQuery query, CancellationToken ct) =>
        Result.Success(await categories.ListAsync(query.IsActive, ct));
}
