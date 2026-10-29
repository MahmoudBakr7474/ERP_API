using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Customers.SetCustomerCredit;

public sealed class SetCustomerCreditValidator : AbstractValidator<SetCustomerCreditCommand>
{
    public SetCustomerCreditValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.PaymentTermDays).InclusiveBetween(0, 365);
        RuleFor(x => x.CreditLimit!.Value).GreaterThanOrEqualTo(0).OverridePropertyName("CreditLimit").When(x => x.CreditLimit.HasValue);
        RuleFor(x => x.CreditHoldReason).NotEmpty().When(x => x.IsOnCreditHold).MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
