using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.ListSourcePostings;

public sealed class ListSourcePostingsValidator : AbstractValidator<ListSourcePostingsQuery>
{
    public ListSourcePostingsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SourceType).IsInEnum();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}
