using FluentValidation;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Accounts.CreateAccount;

public sealed class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(Account.MaxCodeLength);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(Account.MaxNameLength);
        RuleFor(x => x.NameEn).MaximumLength(Account.MaxNameLength);
        RuleFor(x => x.ParentId).NotEqual(Guid.Empty).When(x => x.ParentId.HasValue);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Section).IsInEnum();
        RuleFor(x => x.PartyType).IsInEnum();
    }
}
