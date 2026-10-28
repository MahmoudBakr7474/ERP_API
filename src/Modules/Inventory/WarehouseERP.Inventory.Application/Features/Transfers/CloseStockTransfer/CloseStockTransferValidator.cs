using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Transfers.CloseStockTransfer;

public sealed class CloseStockTransferValidator : AbstractValidator<CloseStockTransferCommand>
{
    public CloseStockTransferValidator()
    {
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
