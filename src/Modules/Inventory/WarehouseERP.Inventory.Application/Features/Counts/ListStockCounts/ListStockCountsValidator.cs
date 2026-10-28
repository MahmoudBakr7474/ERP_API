using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Counts.ListStockCounts;

public sealed class ListStockCountsValidator : AbstractValidator<ListStockCountsQuery>
{
    public ListStockCountsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}
