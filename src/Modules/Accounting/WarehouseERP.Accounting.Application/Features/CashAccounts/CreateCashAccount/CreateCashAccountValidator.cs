using FluentValidation;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.CreateCashAccount;

public sealed class CreateCashAccountValidator : AbstractValidator<CreateCashAccountCommand>
{
    public CreateCashAccountValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(CashAccount.MaxCodeLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(CashAccount.MaxNameLength);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.BankName).MaximumLength(CashAccount.MaxBankNameLength);
        RuleFor(x => x.BankAccountNumber).MaximumLength(CashAccount.MaxAccountNumberLength);
    }
}
