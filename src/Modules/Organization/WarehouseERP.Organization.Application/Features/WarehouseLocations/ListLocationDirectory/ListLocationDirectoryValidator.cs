using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.ListLocationDirectory;

public sealed class ListLocationDirectoryValidator : AbstractValidator<ListLocationDirectoryQuery>
{
    public ListLocationDirectoryValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.WarehouseType).IsInEnum().When(x => x.WarehouseType.HasValue);
        RuleFor(x => x.Sort).IsInEnum().When(x => x.Sort.HasValue);
    }
}
