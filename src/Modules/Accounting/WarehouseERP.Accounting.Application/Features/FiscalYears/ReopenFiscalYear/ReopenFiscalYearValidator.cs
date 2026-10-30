using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalYear;

public sealed class ReopenFiscalYearValidator : AbstractValidator<ReopenFiscalYearCommand>
{
    public ReopenFiscalYearValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(FiscalYear.MaxNoteLength);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
