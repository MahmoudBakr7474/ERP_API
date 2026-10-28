using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Counts.CancelStockCount;

public sealed class CancelStockCountValidator : AbstractValidator<CancelStockCountCommand>
{
    public CancelStockCountValidator()
    {
        RuleFor(x => x.CountId).NotEmpty();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
