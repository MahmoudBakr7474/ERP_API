using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Geography.CreateCity;

// Input shape only; the Arabic name is unique within the governorate, guarded by the database.
public sealed class CreateCityValidator : AbstractValidator<CreateCityCommand>
{
    public CreateCityValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.GovernorateId).NotEmpty();
        RuleFor(x => x.NameAr).MustBeArabicName();
        RuleFor(x => x.NameEn).MustBeEnglishName();
    }
}
