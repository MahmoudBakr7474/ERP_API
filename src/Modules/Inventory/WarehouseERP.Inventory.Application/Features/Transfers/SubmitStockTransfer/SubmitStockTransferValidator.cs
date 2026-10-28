using FluentValidation;
using WarehouseERP.Inventory.Application.Shared;

namespace WarehouseERP.Inventory.Application.Features.Transfers.SubmitStockTransfer;

public sealed class SubmitStockTransferValidator : AbstractValidator<SubmitStockTransferCommand>
{
    public SubmitStockTransferValidator()
    {
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
