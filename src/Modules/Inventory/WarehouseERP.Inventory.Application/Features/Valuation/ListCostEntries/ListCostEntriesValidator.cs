using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Valuation.ListCostEntries;

public sealed class ListCostEntriesValidator : AbstractValidator<ListCostEntriesQuery>
{
    public ListCostEntriesValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
