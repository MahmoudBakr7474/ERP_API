using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.Accounts.ListAccounts;

public sealed class ListAccountsValidator : AbstractValidator<ListAccountsQuery>
{
    public ListAccountsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ParentId).NotEqual(Guid.Empty).When(x => x.ParentId.HasValue);
    }
}
