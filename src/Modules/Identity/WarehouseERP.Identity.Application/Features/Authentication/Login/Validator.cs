using FluentValidation;
using WarehouseERP.Identity.Application.Shared;

namespace WarehouseERP.Identity.Application.Features.Authentication.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).PresentPassword();
    }
}
