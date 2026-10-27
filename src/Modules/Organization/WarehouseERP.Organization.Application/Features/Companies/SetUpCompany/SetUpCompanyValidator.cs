using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Companies.SetUpCompany;

public sealed class SetUpCompanyValidator : AbstractValidator<SetUpCompanyCommand>
{
    public SetUpCompanyValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        // varchar(20) column: ASCII only, so nothing is lost on save.
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.BaseCurrencyCode).NotEmpty().Matches("^[A-Za-z]{3}$");
        RuleFor(x => x.TimeZoneId).MustBeTimeZone();
        RuleFor(x => x.TaxRegistrationNumber).MustBeTaxRegistrationNumber();
    }
}
