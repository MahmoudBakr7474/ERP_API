using FluentValidation;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.StartBankReconciliation;

public sealed class StartBankReconciliationValidator : AbstractValidator<StartBankReconciliationCommand>
{
    public StartBankReconciliationValidator()
    {
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.StatementDate).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
        RuleFor(x => x.StatementBalance).Must(b => decimal.Round(b, 4) == b).WithMessage("The statement balance has at most 4 decimals.");
    }
}
