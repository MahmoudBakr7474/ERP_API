using FluentValidation;
using WarehouseERP.Organization.Application.Shared;

namespace WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;

public sealed class UpdateCompanyValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.TimeZoneId).MustBeTimeZone();
        RuleFor(x => x.TaxRegistrationNumber).MustBeTaxRegistrationNumber();
        RuleFor(x => x.RowVersion).MustBeRowVersion();
        RuleFor(x => x.CommercialRegistrationNumber).MustBeCommercialRegistrationNumber();
        RuleFor(x => x.CommercialRegistry).MaximumLength(100);
        RuleFor(x => x.Phone).MustBePhone();
        RuleFor(x => x.Email).MustBeEmail().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AddressLine).MaximumLength(200);
        RuleFor(x => x.CityId).NotEqual(Guid.Empty).When(x => x.CityId.HasValue);
    }
}
