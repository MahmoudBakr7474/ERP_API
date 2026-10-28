using FluentValidation;
using WarehouseERP.Organization.Application.Shared;

namespace WarehouseERP.Organization.Application.Features.Geography.UpdateCountry;

public sealed class UpdateCountryValidator : AbstractValidator<UpdateCountryCommand>
{
    public UpdateCountryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.CountryId).NotEmpty();
        RuleFor(x => x.NameAr).MustBeArabicName();
        RuleFor(x => x.NameEn).MustBeEnglishName();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
