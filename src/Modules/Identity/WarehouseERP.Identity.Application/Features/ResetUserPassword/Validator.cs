using FluentValidation;
using WarehouseERP.Identity.Application.Shared;

namespace WarehouseERP.Identity.Application.Features.ResetUserPassword;

public sealed class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).PresentPassword();
    }
}
