using FluentValidation;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Shifts.RecordPosCashMovement;

public sealed class RecordPosCashMovementValidator : AbstractValidator<RecordPosCashMovementCommand>
{
    public RecordPosCashMovementValidator()
    {
        RuleFor(x => x.MovementType).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(Money.MaxAmount)
            .Must(a => Money.FitsScale(a, Money.AmountScale)).WithMessage("'Amount' may have at most 4 decimals.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(PosShift.MaxReasonLength);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
