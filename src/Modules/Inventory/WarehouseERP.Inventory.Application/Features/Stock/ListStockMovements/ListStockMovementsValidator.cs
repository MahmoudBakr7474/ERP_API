using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockMovements;

public sealed class ListStockMovementsValidator : AbstractValidator<ListStockMovementsQuery>
{
    public ListStockMovementsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.LocationId).NotEqual(Guid.Empty).When(x => x.LocationId.HasValue);
        RuleFor(x => x.ToUtc).GreaterThan(x => x.FromUtc).When(x => x.FromUtc.HasValue && x.ToUtc.HasValue);
    }
}
