using FluentValidation;

namespace WarehouseERP.Catalog.Application.Features.Units.CreateUnit;

// Input shape only; the unique code is guarded again by the database index.
public sealed class CreateUnitValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        // varchar(20) column: ASCII only, so nothing is lost on save.
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameEn).MaximumLength(100);
    }
}
