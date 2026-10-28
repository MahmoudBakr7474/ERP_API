using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.ListSuppliers;

public sealed class ListSuppliersValidator : AbstractValidator<ListSuppliersQuery>
{
    public ListSuppliersValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.CountryCode).Matches(SupplierRules.CountryPattern).When(x => x.CountryCode is not null);
    }
}
