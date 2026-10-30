using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.CancelBankReconciliation;

public sealed class CancelBankReconciliationValidator : AbstractValidator<CancelBankReconciliationCommand>
{
    public CancelBankReconciliationValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
