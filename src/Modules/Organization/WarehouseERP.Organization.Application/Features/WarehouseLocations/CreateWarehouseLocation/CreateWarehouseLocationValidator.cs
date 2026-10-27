using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.CreateWarehouseLocation;

// Input shape only; the unique code per warehouse is guarded again by the database index.
public sealed class CreateWarehouseLocationValidator : AbstractValidator<CreateWarehouseLocationCommand>
{
    public CreateWarehouseLocationValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.WarehouseId).NotEmpty();
        // varchar(50) column: ASCII only, so nothing is lost on save. Dots allow bin codes such as A.01.02.
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9_.-]+$");
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameEn).MaximumLength(150);
    }
}
