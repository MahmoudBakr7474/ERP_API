using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ListSupplierInvoices;

public sealed class ListSupplierInvoicesValidator : AbstractValidator<ListSupplierInvoicesQuery>
{
    public ListSupplierInvoicesValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
