using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.Reports.ListOrganizationDataIssues;

public sealed class ListOrganizationDataIssuesValidator : AbstractValidator<ListOrganizationDataIssuesQuery>
{
    public ListOrganizationDataIssuesValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Issue).IsInEnum().When(x => x.Issue.HasValue);
    }
}
