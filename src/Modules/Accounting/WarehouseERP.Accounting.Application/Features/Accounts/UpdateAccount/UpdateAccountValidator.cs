using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Accounts.UpdateAccount;

public sealed class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(Account.MaxNameLength);
        RuleFor(x => x.NameEn).MaximumLength(Account.MaxNameLength);
        RuleFor(x => x.Section).IsInEnum();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
