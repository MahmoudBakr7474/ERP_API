using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CreateFiscalYear;

public sealed class CreateFiscalYearValidator : AbstractValidator<CreateFiscalYearCommand>
{
    public CreateFiscalYearValidator() => RuleFor(x => x.StartYear).InclusiveBetween(2000, 2100);
}
