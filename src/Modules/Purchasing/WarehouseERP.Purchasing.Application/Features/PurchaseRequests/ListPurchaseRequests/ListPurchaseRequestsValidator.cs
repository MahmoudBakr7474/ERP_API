using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ListPurchaseRequests;

public sealed class ListPurchaseRequestsValidator : AbstractValidator<ListPurchaseRequestsQuery>
{
    public ListPurchaseRequestsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
