using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.Settings.UpdateAccountingSettings;

public sealed class UpdateAccountingSettingsValidator : AbstractValidator<UpdateAccountingSettingsCommand>
{
    public UpdateAccountingSettingsValidator()
    {
        RuleFor(x => x.FiscalYearStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.CutoverDate).Must(d => d.Day == 1).WithMessage("The accounting start date is the first day of a month.");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
