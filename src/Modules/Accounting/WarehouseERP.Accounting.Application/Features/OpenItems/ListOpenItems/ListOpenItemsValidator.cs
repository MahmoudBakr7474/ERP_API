using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.OpenItems.ListOpenItems;

public sealed class ListOpenItemsValidator : AbstractValidator<ListOpenItemsQuery>
{
    public ListOpenItemsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.PartyType).IsInEnum();
        RuleFor(x => x.PartyType).NotNull().When(x => x.PartyId.HasValue).WithMessage("A party is named with its type.");
        RuleFor(x => x.PartyId).NotEqual(Guid.Empty).When(x => x.PartyId.HasValue);
        RuleFor(x => x.AccountId).NotEqual(Guid.Empty).When(x => x.AccountId.HasValue);
    }
}
