using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Pos.Application.Features.CardIncidents.ListPosCardIncidents;

public sealed class ListPosCardIncidentsValidator : AbstractValidator<ListPosCardIncidentsQuery>
{
    public ListPosCardIncidentsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.TerminalId).NotEqual(Guid.Empty).When(x => x.TerminalId.HasValue);
        RuleFor(x => x.ShiftId).NotEqual(Guid.Empty).When(x => x.ShiftId.HasValue);
        RuleFor(x => x.Status!.Value).IsInEnum().OverridePropertyName("Status").When(x => x.Status.HasValue);
    }
}
