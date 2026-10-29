using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockReservations;

public sealed class ListStockReservationsValidator : AbstractValidator<ListStockReservationsQuery>
{
    public ListStockReservationsValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).When(x => x.ProductId.HasValue);
    }
}
