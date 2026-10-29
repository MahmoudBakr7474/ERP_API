using FluentValidation;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Shifts.ClosePosShift;

public sealed class ClosePosShiftValidator : AbstractValidator<ClosePosShiftCommand>
{
    public ClosePosShiftValidator()
    {
        RuleFor(x => x.CountedCash).GreaterThanOrEqualTo(0).LessThanOrEqualTo(Money.MaxAmount)
            .Must(a => Money.FitsScale(a, Money.AmountScale)).WithMessage("'Counted Cash' may have at most 4 decimals.");
        RuleFor(x => x.Note).MaximumLength(PosShift.MaxNoteLength);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
