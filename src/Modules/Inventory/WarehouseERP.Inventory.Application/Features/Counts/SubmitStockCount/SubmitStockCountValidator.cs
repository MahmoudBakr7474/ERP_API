using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Counts.SubmitStockCount;

public sealed class SubmitStockCountValidator : AbstractValidator<SubmitStockCountCommand>
{
    public SubmitStockCountValidator()
    {
        RuleFor(x => x.CountId).NotEmpty();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
