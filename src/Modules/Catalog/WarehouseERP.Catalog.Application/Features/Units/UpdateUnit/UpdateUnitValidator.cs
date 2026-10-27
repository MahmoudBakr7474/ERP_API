using FluentValidation;
using WarehouseERP.Catalog.Application.Shared;

namespace WarehouseERP.Catalog.Application.Features.Units.UpdateUnit;

public sealed class UpdateUnitValidator : AbstractValidator<UpdateUnitCommand>
{
    public UpdateUnitValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameEn).MaximumLength(100);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
