using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Geography.CreateCountry;

// Input shape only; the unique code and Arabic name are guarded again by the database.
public sealed class CreateCountryValidator : AbstractValidator<CreateCountryCommand>
{
    public CreateCountryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.Code).NotEmpty().Matches(GeographyRules.CountryCodePattern);
        RuleFor(x => x.NameAr).MustBeArabicName();
        RuleFor(x => x.NameEn).MustBeEnglishName();
    }
}
