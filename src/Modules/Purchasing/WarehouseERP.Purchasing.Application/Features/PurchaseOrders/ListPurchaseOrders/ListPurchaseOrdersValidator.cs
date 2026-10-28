using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ListPurchaseOrders;

public sealed class ListPurchaseOrdersValidator : AbstractValidator<ListPurchaseOrdersQuery>
{
    public ListPurchaseOrdersValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
