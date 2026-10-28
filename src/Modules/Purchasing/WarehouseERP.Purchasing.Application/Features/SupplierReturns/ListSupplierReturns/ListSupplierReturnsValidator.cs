using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.ListSupplierReturns;

public sealed class ListSupplierReturnsValidator : AbstractValidator<ListSupplierReturnsQuery>
{
    public ListSupplierReturnsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
