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
    }
}
