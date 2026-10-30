using FluentValidation;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalPeriod;

public sealed class ReopenFiscalPeriodValidator : AbstractValidator<ReopenFiscalPeriodCommand>
{
    public ReopenFiscalPeriodValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(FiscalYear.MaxNoteLength);
}
