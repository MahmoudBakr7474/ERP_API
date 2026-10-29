using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Sales.Application.Features.Invoices.ListSalesInvoices;

public sealed class ListSalesInvoicesValidator : AbstractValidator<ListSalesInvoicesQuery>
{
    public ListSalesInvoicesValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
