using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.Warehouses.ListWarehouses;

public sealed class ListWarehousesValidator : AbstractValidator<ListWarehousesQuery>
{
    public ListWarehousesValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.Sort).IsInEnum().When(x => x.Sort.HasValue);
    }
}
