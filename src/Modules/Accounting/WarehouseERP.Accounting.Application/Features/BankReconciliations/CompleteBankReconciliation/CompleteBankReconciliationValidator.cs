using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.CompleteBankReconciliation;

public sealed class CompleteBankReconciliationValidator : AbstractValidator<CompleteBankReconciliationCommand>
{
    public CompleteBankReconciliationValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
