using FluentValidation;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Shifts.OpenPosShift;

public sealed class OpenPosShiftValidator : AbstractValidator<OpenPosShiftCommand>
{
    public OpenPosShiftValidator()
    {
        RuleFor(x => x.TerminalId).NotEmpty();
        RuleFor(x => x.OpeningFloat).GreaterThanOrEqualTo(0).LessThanOrEqualTo(Money.MaxAmount)
            .Must(a => Money.FitsScale(a, Money.AmountScale)).WithMessage("'Opening Float' may have at most 4 decimals.");
    }
}
