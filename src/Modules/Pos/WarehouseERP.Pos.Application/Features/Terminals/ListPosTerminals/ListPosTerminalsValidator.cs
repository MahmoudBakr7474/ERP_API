using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Pos.Application.Features.Terminals.ListPosTerminals;

public sealed class ListPosTerminalsValidator : AbstractValidator<ListPosTerminalsQuery>
{
    public ListPosTerminalsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
