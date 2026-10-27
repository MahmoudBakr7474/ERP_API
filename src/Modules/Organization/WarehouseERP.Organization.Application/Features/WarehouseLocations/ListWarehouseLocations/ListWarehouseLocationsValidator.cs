using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.ListWarehouseLocations;

public sealed class ListWarehouseLocationsValidator : AbstractValidator<ListWarehouseLocationsQuery>
{
    public ListWarehouseLocationsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
