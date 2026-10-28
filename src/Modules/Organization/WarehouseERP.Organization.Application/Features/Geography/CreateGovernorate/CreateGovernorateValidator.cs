using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Geography.CreateGovernorate;

// Input shape only; the Arabic name is unique within the country, guarded by the database.
public sealed class CreateGovernorateValidator : AbstractValidator<CreateGovernorateCommand>
{
    public CreateGovernorateValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.CountryCode).NotEmpty().Matches(GeographyRules.CountryCodePattern);
        RuleFor(x => x.NameAr).MustBeArabicName();
        RuleFor(x => x.NameEn).MustBeEnglishName();
    }
}
