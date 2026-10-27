using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;
using WarehouseERP.Catalog.Application.Contracts.Categories;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Categories.GetCategory;

public sealed class GetCategoryHandler(ICategoryReadStore categories) : IQueryHandler<GetCategoryQuery, CategoryDetailsResponse>
{
    public async Task<Result<CategoryDetailsResponse>> HandleAsync(GetCategoryQuery query, CancellationToken ct) =>
        await categories.GetAsync(query.CategoryId, ct) is { } category ? category : CategoryErrors.NotFound;
}
