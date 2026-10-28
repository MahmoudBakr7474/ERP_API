using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Transfers.CancelStockTransfer;

public sealed class CancelStockTransferValidator : AbstractValidator<CancelStockTransferCommand>
{
    public CancelStockTransferValidator()
    {
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
