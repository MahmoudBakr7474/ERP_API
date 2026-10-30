using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.TickBankReconciliationLines;

public sealed class TickBankReconciliationLinesValidator : AbstractValidator<TickBankReconciliationLinesCommand>
{
    public TickBankReconciliationLinesValidator()
    {
        RuleFor(x => x.JournalLineIds).NotNull().Must(ids => ids.Count <= 5000 && ids.Distinct().Count() == ids.Count)
            .WithMessage("Each line is ticked once; at most 5000 lines.");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
