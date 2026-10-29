using FluentValidation;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Customers.UpdateCustomer;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.CountryCode).NotEmpty().Matches(CustomerRules.CountryPattern);
        RuleFor(x => x.TaxRegistrationNumber).MaximumLength(50);
        RuleFor(x => x.CommercialRegistrationNumber).MaximumLength(50);
        RuleFor(x => x.CommercialRegistry).MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Mobile).MaximumLength(30);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Contacts).Must(c => c is null || c.Count <= Customer.MaxContacts).WithMessage("A customer has at most 20 contacts.");
        RuleFor(x => x.Addresses).Must(a => a is null || a.Count <= Customer.MaxAddresses).WithMessage("A customer has at most 10 addresses.");
        RuleForEach(x => x.Contacts).Cascade(CascadeMode.Continue).ChildRules(CustomerRules.Contact);
        RuleForEach(x => x.Addresses).Cascade(CascadeMode.Continue).ChildRules(CustomerRules.Address);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
