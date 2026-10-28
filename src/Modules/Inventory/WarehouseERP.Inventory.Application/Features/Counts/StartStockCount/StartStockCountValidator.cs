using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Counts.StartStockCount;

public sealed class StartStockCountValidator : AbstractValidator<StartStockCountCommand>
{
    public StartStockCountValidator()
    {
        RuleFor(x => x.CountId).NotEmpty();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
