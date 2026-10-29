using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Sales.Application.Features.Prices.ListSalesPrices;

public sealed class ListSalesPricesValidator : AbstractValidator<ListSalesPricesQuery>
{
    public ListSalesPricesValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).When(x => x.ProductId.HasValue);
        RuleFor(x => x.ProductUnitId).NotEqual(Guid.Empty).When(x => x.ProductUnitId.HasValue);
    }
}
