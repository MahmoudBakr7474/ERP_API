using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Branches.CreateBranch;

// Input shape only; the unique code per company is guarded again by the database index.
public sealed class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        // varchar(30) column: ASCII only, so nothing is lost on save.
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameEn).MaximumLength(150);
    }
}
