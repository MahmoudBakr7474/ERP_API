using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Counts.ApproveStockCount;

public sealed class ApproveStockCountValidator : AbstractValidator<ApproveStockCountCommand>
{
    public ApproveStockCountValidator()
    {
        RuleFor(x => x.CountId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per action, the same on a retry).");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
