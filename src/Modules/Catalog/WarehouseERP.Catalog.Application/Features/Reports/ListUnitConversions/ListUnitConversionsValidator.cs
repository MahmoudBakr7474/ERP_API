using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Catalog.Application.Features.Reports.ListUnitConversions;

public sealed class ListUnitConversionsValidator : AbstractValidator<ListUnitConversionsQuery>
{
    public ListUnitConversionsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.UnitId).NotEqual(Guid.Empty).When(x => x.UnitId.HasValue);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
    }
}
