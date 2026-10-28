using FluentValidation;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.CreateSupplier;

// Input shape only; registration numbers are validated once normalized, and the database guards every unique key.
public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.CountryCode).NotEmpty().Matches(SupplierRules.CountryPattern);
        RuleFor(x => x.TaxRegistrationNumber).MaximumLength(50);
        RuleFor(x => x.CommercialRegistrationNumber).MaximumLength(50);
        RuleFor(x => x.CommercialRegistry).MaximumLength(100);
        RuleFor(x => x.PaymentTermDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Contacts).Must(c => c is null || c.Count <= Supplier.MaxContacts).WithMessage("A supplier has at most 20 contacts.");
        RuleFor(x => x.Addresses).Must(a => a is null || a.Count <= Supplier.MaxAddresses).WithMessage("A supplier has at most 10 addresses.");
        RuleForEach(x => x.Contacts).Cascade(CascadeMode.Continue).ChildRules(SupplierRules.Contact);
        RuleForEach(x => x.Addresses).Cascade(CascadeMode.Continue).ChildRules(SupplierRules.Address);
    }
}
