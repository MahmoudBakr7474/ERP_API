using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Identity.Application.Features.SecurityEvents.ListSecurityEvents;

public sealed class ListSecurityEventsValidator : AbstractValidator<ListSecurityEventsQuery>
{
    public ListSecurityEventsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ToUtc).GreaterThan(x => x.FromUtc).When(x => x.FromUtc.HasValue && x.ToUtc.HasValue);
    }
}
