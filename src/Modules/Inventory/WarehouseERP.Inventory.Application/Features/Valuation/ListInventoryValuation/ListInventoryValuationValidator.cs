using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Valuation.ListInventoryValuation;

public sealed class ListInventoryValuationValidator : AbstractValidator<ListInventoryValuationQuery>
{
    public ListInventoryValuationValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).When(x => x.ProductId.HasValue);
    }
}
