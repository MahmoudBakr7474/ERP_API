using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.ListWarehouses;

public sealed class ListWarehousesValidator : AbstractValidator<ListWarehousesQuery>
{
    public ListWarehousesValidator(IOptions<PaginationOptions> pagination) => this.AddPagingRules(pagination.Value);
}
