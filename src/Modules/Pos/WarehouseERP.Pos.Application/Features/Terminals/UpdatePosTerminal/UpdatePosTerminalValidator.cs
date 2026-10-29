using FluentValidation;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Terminals.UpdatePosTerminal;

public sealed class UpdatePosTerminalValidator : AbstractValidator<UpdatePosTerminalCommand>
{
    public UpdatePosTerminalValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(PosTerminal.MaxNameLength);
        RuleFor(x => x.SellingLocationId).NotEqual(Guid.Empty).When(x => x.SellingLocationId.HasValue);
        RuleFor(x => x.ReceiptHeader).MaximumLength(PosTerminal.MaxReceiptTextLength);
        RuleFor(x => x.ReceiptFooter).MaximumLength(PosTerminal.MaxReceiptTextLength);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
