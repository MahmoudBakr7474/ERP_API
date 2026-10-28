using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Counts.RejectStockCount;

public sealed class RejectStockCountValidator : AbstractValidator<RejectStockCountCommand>
{
    public RejectStockCountValidator()
    {
        RuleFor(x => x.CountId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
