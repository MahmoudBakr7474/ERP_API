using FluentValidation;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Terminals.CreatePosTerminal;

public sealed class CreatePosTerminalValidator : AbstractValidator<CreatePosTerminalCommand>
{
    public CreatePosTerminalValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(PosTerminal.MaxCodeLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(PosTerminal.MaxNameLength);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.SellingLocationId).NotEqual(Guid.Empty).When(x => x.SellingLocationId.HasValue);
        RuleFor(x => x.ReceiptHeader).MaximumLength(PosTerminal.MaxReceiptTextLength);
        RuleFor(x => x.ReceiptFooter).MaximumLength(PosTerminal.MaxReceiptTextLength);
    }
}
