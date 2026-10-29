using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Pos.Application.Features.Shifts.ListPosShifts;

public sealed class ListPosShiftsValidator : AbstractValidator<ListPosShiftsQuery>
{
    public ListPosShiftsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.TerminalId).NotEqual(Guid.Empty).When(x => x.TerminalId.HasValue);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.CashierUserId).NotEqual(Guid.Empty).When(x => x.CashierUserId.HasValue);
        RuleFor(x => x.Status!.Value).IsInEnum().OverridePropertyName("Status").When(x => x.Status.HasValue);
    }
}
