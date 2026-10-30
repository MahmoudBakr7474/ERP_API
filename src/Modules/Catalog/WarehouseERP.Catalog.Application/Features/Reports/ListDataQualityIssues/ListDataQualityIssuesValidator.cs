using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Catalog.Application.Features.Reports.ListDataQualityIssues;

public sealed class ListDataQualityIssuesValidator : AbstractValidator<ListDataQualityIssuesQuery>
{
    public ListDataQualityIssuesValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Issue).IsInEnum().When(x => x.Issue.HasValue);
    }
}
