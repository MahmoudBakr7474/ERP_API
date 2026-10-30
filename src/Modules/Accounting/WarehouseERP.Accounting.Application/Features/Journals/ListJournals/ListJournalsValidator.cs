using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.Journals.ListJournals;

public sealed class ListJournalsValidator : AbstractValidator<ListJournalsQuery>
{
    public ListJournalsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}
