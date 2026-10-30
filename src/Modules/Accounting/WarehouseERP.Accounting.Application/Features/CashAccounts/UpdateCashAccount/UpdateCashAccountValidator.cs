using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.UpdateCashAccount;

public sealed class UpdateCashAccountValidator : AbstractValidator<UpdateCashAccountCommand>
{
    public UpdateCashAccountValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(CashAccount.MaxNameLength);
        RuleFor(x => x.BankName).MaximumLength(CashAccount.MaxBankNameLength);
        RuleFor(x => x.BankAccountNumber).MaximumLength(CashAccount.MaxAccountNumberLength);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
