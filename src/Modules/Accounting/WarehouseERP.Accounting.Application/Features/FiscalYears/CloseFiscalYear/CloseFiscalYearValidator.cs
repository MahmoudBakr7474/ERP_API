using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalYear;

public sealed class CloseFiscalYearValidator : AbstractValidator<CloseFiscalYearCommand>
{
    public CloseFiscalYearValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
