using FluentValidation;
using WarehouseERP.Organization.Application.Shared;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.UpdateWarehouseLocation;

public sealed class UpdateWarehouseLocationValidator : AbstractValidator<UpdateWarehouseLocationCommand>
{
    public UpdateWarehouseLocationValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameEn).MaximumLength(150);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
