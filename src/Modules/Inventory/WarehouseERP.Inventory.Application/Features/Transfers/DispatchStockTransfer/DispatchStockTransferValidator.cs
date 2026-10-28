using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Transfers.DispatchStockTransfer;

public sealed class DispatchStockTransferValidator : AbstractValidator<DispatchStockTransferCommand>
{
    public DispatchStockTransferValidator()
    {
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per action, the same on a retry).");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
