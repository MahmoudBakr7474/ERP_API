using FluentValidation;
using WarehouseERP.Organization.Application.Shared;

namespace WarehouseERP.Organization.Application.Features.Geography.UpdateCity;

public sealed class UpdateCityValidator : AbstractValidator<UpdateCityCommand>
{
    public UpdateCityValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.CityId).NotEmpty();
        RuleFor(x => x.NameAr).MustBeArabicName();
        RuleFor(x => x.NameEn).MustBeEnglishName();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
