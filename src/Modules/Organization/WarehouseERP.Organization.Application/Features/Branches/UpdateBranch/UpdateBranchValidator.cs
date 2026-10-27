using FluentValidation;
using WarehouseERP.Organization.Application.Shared;

namespace WarehouseERP.Organization.Application.Features.Branches.UpdateBranch;

public sealed class UpdateBranchValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameEn).MaximumLength(150);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
