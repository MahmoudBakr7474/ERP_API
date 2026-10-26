using FluentValidation;
using WarehouseERP.Identity.Application.Shared;

namespace WarehouseERP.Identity.Application.Features.Users.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.DisplayName).ValidDisplayName();
        RuleFor(x => x.Email).ValidOptionalEmail();
        RuleFor(x => x.Password).PresentPassword();
        RuleFor(x => x.RoleIds).NotNull();
        RuleForEach(x => x.RoleIds).NotEmpty();
        RuleForEach(x => x.BranchIds).NotEmpty();
        RuleForEach(x => x.WarehouseIds).NotEmpty();
    }
}
