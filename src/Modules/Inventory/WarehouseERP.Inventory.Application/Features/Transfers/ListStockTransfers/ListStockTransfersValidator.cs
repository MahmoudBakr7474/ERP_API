using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Transfers.ListStockTransfers;

public sealed class ListStockTransfersValidator : AbstractValidator<ListStockTransfersQuery>
{
    public ListStockTransfersValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
