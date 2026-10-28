using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ListGoodsReceipts;

public sealed class ListGoodsReceiptsValidator : AbstractValidator<ListGoodsReceiptsQuery>
{
    public ListGoodsReceiptsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
