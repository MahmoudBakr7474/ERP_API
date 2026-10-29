using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Sales.Application.Features.Customers.ListCustomers;

public sealed class ListCustomersValidator : AbstractValidator<ListCustomersQuery>
{
    public ListCustomersValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.CustomerType).IsInEnum().When(x => x.CustomerType.HasValue);
        RuleFor(x => x.CountryCode).Matches(CustomerRules.CountryPattern).When(x => x.CountryCode is not null);
    }
}
