using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Pos.Application.Features.Returns.ListPosReturns;

public sealed class ListPosReturnsValidator : AbstractValidator<ListPosReturnsQuery>
{
    public ListPosReturnsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.TerminalId).NotEqual(Guid.Empty).When(x => x.TerminalId.HasValue);
        RuleFor(x => x.ShiftId).NotEqual(Guid.Empty).When(x => x.ShiftId.HasValue);
        RuleFor(x => x.SaleId).NotEqual(Guid.Empty).When(x => x.SaleId.HasValue);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
    }
}
