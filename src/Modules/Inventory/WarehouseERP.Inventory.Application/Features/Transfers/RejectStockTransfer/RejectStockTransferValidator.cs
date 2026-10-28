using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Transfers.RejectStockTransfer;

public sealed class RejectStockTransferValidator : AbstractValidator<RejectStockTransferCommand>
{
    public RejectStockTransferValidator()
    {
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
