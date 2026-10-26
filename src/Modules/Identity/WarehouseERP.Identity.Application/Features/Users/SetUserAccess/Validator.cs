using FluentValidation;

namespace WarehouseERP.Identity.Application.Features.Users.SetUserAccess;

public sealed class SetUserAccessValidator : AbstractValidator<SetUserAccessCommand>
{
    public SetUserAccessValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BranchIds).NotNull();
        RuleFor(x => x.WarehouseIds).NotNull();
        RuleForEach(x => x.BranchIds).NotEmpty();
        RuleForEach(x => x.WarehouseIds).NotEmpty();
    }
}
