using FluentValidation;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.Settings.UpdatePosSettings;

public sealed class UpdatePosSettingsValidator : AbstractValidator<UpdatePosSettingsCommand>
{
    public UpdatePosSettingsValidator()
    {
        RuleFor(x => x.MaxLineDiscountPercent).InclusiveBetween(0, 100)
            .Must(p => Money.FitsScale(p, Money.PercentScale)).WithMessage("'Max Line Discount Percent' may have at most 4 decimals.");
        RuleFor(x => x.ReturnWindowDays).InclusiveBetween(0, PosSettings.MaxReturnWindowDays);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
