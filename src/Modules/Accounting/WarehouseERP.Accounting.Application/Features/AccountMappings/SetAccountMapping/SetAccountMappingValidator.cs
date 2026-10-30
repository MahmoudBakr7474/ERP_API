using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.AccountMappings.SetAccountMapping;

public sealed class SetAccountMappingValidator : AbstractValidator<SetAccountMappingCommand>
{
    public SetAccountMappingValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.RowVersion!).MustBeRowVersion().When(x => x.RowVersion is not null);
    }
}
